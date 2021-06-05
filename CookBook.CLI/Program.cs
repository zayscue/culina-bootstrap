using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json;

namespace Culina.Bootstrap.CookBook.CLI
{
    class Program
    {
        private static readonly IDictionary<string, string> AUDIENCES = new Dictionary<string, string>()
        {
            { "localhost", "https://api.culina.dev/cookbook/" },
            { "api.culina.io", "https://api.culina.io/cookbook/" }
        };


        private static HttpClient Client = new HttpClient();


        static async Task<int> Main(string[] args)
        {
            var apiEndpointAliases = new string[] { "--endpoint", "-e" };
            var apiEndpointOption = new Option<string>(
                apiEndpointAliases,
                () => "https://localhost:5003/cookbook/",
                description: "The base address for the cook book api")
            {
                IsRequired = true
            };

            var fileOptionAliases = new string[] { "--recipes", "-r" };
            var fileOption = new Option<string>(
                fileOptionAliases,
                description: "The path to the recipes csv file")
            {
                IsRequired = true,

            };

            var credentialsAliases = new string[] { "--credentials", "-c" };
            var credentialsOption = new Option<string>(
                credentialsAliases,
                () => "~/.culina/creds.json",
                description: "The path to the credentials json file")
            {
                IsRequired = true
            };

            var rootCommand = new RootCommand
            {
                apiEndpointOption,
                credentialsOption,
                fileOption
            };
            rootCommand.Description = "A tool for seeding the data of the culina cook book service using the provided csv file.";
            rootCommand.Handler = CommandHandler.Create<string, string, string, IConsole>(async (endpoint, recipes, credentials, console) =>
            {
                var endpointUri = new Uri(endpoint);
                Client.BaseAddress = endpointUri;

                var endpointDomain = endpointUri.Host;
                var audience = AUDIENCES[endpointDomain];
                var credentialsFilePath = Path.GetFullPath(credentials.Replace("~",
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
                var credentialsFileContents = await File.ReadAllTextAsync(credentialsFilePath);
                var credentialsDictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                    credentialsFileContents);
                var credentialsObj = new Credentials
                {
                    Audience =  audience,
                    STS = credentialsDictionary["sts"],
                    ClientId = credentialsDictionary["client_id"],
                    ClientSecret = credentialsDictionary["client_secret"]
                };
                var tokenService = new TokenService(credentialsObj);

                var recipesFilePath = Path.GetFullPath(recipes.Replace("~",
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

                using (var reader = new StreamReader(recipesFilePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<RecipeMap>();
                    await csv.ReadAsync();
                    csv.ReadHeader();
                    var line = 1;
                    var startLine = 1;
                    while (await csv.ReadAsync())
                    {
                        line++;
                        if (line >= startLine)
                        {
                            var record = csv.GetRecord<RecipeRecord>();
                            var command = MapCreateRecipeCommand(record);
                            var result = await CreateRecipe(Client, tokenService, command);
                            if (!result)
                            {
                                console.Out.Write($"The record on line {line} failed.{Environment.NewLine}");
                                console.Out.Write($"{JsonConvert.SerializeObject(command)}{Environment.NewLine}");
                                break;
                            }
                        }
                    }
                }
            });
            return await rootCommand.InvokeAsync(args);
        }

        private static CreateRecipeCommand MapCreateRecipeCommand(RecipeRecord recipeRecord)
        {
            var createRecipeCommand = new CreateRecipeCommand
            {
                Name = recipeRecord.Name,
                Description = recipeRecord.Description,
                EstimatedMinutes = recipeRecord.Minutes,
                Serves = recipeRecord.Serves,
                Steps = recipeRecord.Steps,
                Ingredients = recipeRecord.Ingredients != null && recipeRecord.Ingredients.Count > 0
                    ? recipeRecord.Ingredients.Select(x => new CreateRecipeIngredientCommand {
                        Quantity = x.Quantity,
                        Part = x.Part,
                        Type = x.Type
                    }).ToList()
                    : null,
                Tags = recipeRecord.Tags
            };
            var metadata = new List<CreateRecipeMetadataCommand>();
            if (recipeRecord.Id > 0)
            {
                metadata.Add(new CreateRecipeMetadataCommand
                {
                    Type = "source_id",
                    Value = recipeRecord.Id.ToString()
                });
            }
            if (recipeRecord.ContributorId > 0)
            {
                metadata.Add(new CreateRecipeMetadataCommand
                {
                    Type = "contributor_id",
                    Value = recipeRecord.ContributorId.ToString()
                });
            }
            if (!string.IsNullOrWhiteSpace(recipeRecord.Submitted))
            {
                metadata.Add(new CreateRecipeMetadataCommand
                {
                    Type = "submitted",
                    Value = recipeRecord.Submitted.ToString()
                });
            }
            if (metadata.Count > 0)
            {
                createRecipeCommand.Metadata = metadata;
            }
            if (!string.IsNullOrWhiteSpace(recipeRecord.ImageUrl))
            {
                var imageUrls = new List<string>();
                imageUrls.Add(recipeRecord.ImageUrl);
                createRecipeCommand.ImageUrls = imageUrls;
            }
            if (recipeRecord.NutritionInfo != null && recipeRecord.NutritionInfo.Count > 0 && !string.IsNullOrWhiteSpace(recipeRecord.ServingSize))
            {
                var createRecipeNutritionCommand = new CreateRecipeNutritionCommand
                {
                    ServingSize = recipeRecord.ServingSize,
                    ServingsPerRecipe = recipeRecord.ServingsPerRecipe
                };
                foreach(var nutritionInfo in recipeRecord.NutritionInfo)
                {
                    switch(nutritionInfo.Name)
                    {
                        case "Calories":
                            createRecipeNutritionCommand.Calories = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            break;
                        case "Calories from Fat":
                            createRecipeNutritionCommand.CaloriesFromFat = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.CaloriesFromFatPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                        case "Total Fat":
                            createRecipeNutritionCommand.TotalFat = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.TotalFatPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                        case "Saturated Fat":
                            createRecipeNutritionCommand.SaturatedFat = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.SaturatedFatPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                        case "Cholesterol":
                            createRecipeNutritionCommand.Cholesterol = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.CholesterolPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                       case "Sodium":
                            createRecipeNutritionCommand.Sodium = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.SodiumPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                       case "Total Carbohydrate":
                            createRecipeNutritionCommand.TotalCarbohydrates = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.TotalCarbohydratesPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                       case "Dietary Fiber":
                            createRecipeNutritionCommand.DietaryFiber = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.DietaryFiberPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                       case "Sugars":
                            createRecipeNutritionCommand.Sugar = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.SugarPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                       case "Protein":
                            createRecipeNutritionCommand.Protein = nutritionInfo.Amount.HasValue
                                ? nutritionInfo.Amount.Value
                                : default;
                            createRecipeNutritionCommand.ProteinPdv = nutritionInfo.PercentOfDailyValue.HasValue
                                ? nutritionInfo.PercentOfDailyValue.Value
                                : default;
                            break;
                    }
                }
                createRecipeCommand.Nutrition = createRecipeNutritionCommand;
            }
            return createRecipeCommand;
        }

        private static async Task<bool> CreateRecipe(HttpClient client, TokenService tokenService, CreateRecipeCommand command)
        {
            var (tokenType, accessToken) = await tokenService.GetToken();
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = System.Text.Json.JsonSerializer.Serialize(command, serializeOptions);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(client.BaseAddress, "/cookbook/recipes"),
                Headers =
                {
                    { HttpRequestHeader.Authorization.ToString(), $"{tokenType} {accessToken}" }
                },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}
