using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;

namespace Culina.Bootstrap.CookBook.CLI
{
    public class RecipeMap : ClassMap<RecipeRecord>
    {
        public RecipeMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.Name).Name("name");
            Map(m => m.Minutes).Name("minutes");
            Map(m => m.ContributorId).Name("contributor_id");
            Map(m => m.Submitted).Name("submitted");
            Map(m => m.Tags).Name("tags").TypeConverter<StringListConverter>();
            Map(m => m.Description).Name("description");
            Map(m => m.ImageUrl).Name("image_url");
            Map(m => m.Serves).Name("serves");
            Map(m => m.Yield).Name("yield");
            Map(m => m.ServingSize).Name("serving_size");
            Map(m => m.ServingsPerRecipe).Name("servings_per_recipe");
            Map(m => m.NumberOfSteps).Name("n_steps");
            Map(m => m.Steps).Name("steps").TypeConverter<StringListConverter>();
            Map(m => m.Ingredients).Name("ingredients").TypeConverter<IngredientRecordListConverter>();
            Map(m => m.NumberOfIngredients).Name("n_ingredients");
            Map(m => m.NutritionInfo).Name("nutrition_info").TypeConverter<NutritionInfoRecordListConverter>();
        }
    }

    public class StringListConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<string>>(text);
            }
            catch (Exception)
            {
                var tempStr = text.Replace("[", "").Replace("]", "");
                var tempParts = tempStr.Split("\", \"");
                var length = tempParts.Length;
                tempParts[0] = tempParts[0].Remove(0, 1);
                var lastTempPart = tempParts[length - 1];
                lastTempPart = lastTempPart.Remove(lastTempPart.Length - 1);
                tempParts[length - 1] = lastTempPart;
                return tempParts.ToList();
            }
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return JsonConvert.SerializeObject(value);
        }
    }

    public class IngredientRecordListConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var ingredientRecords = new List<IngredientRecord>();
            var items = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(text);
            foreach (var item in items)
            {
                var ingredientRecord = new IngredientRecord();
                if (item.ContainsKey("quantity"))
                {
                    ingredientRecord.Quantity = item["quantity"];
                }
                if (item.ContainsKey("part"))
                {
                    ingredientRecord.Part = item["part"];
                }
                if (item.ContainsKey("type"))
                {
                    ingredientRecord.Type = item["type"];
                }
                ingredientRecords.Add(ingredientRecord);
            }
            return ingredientRecords;
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            var ingredientRecords = value as List<IngredientRecord>;
            if (ingredientRecords == null) return null;
            var items = new List<Dictionary<string, string>>();
            foreach (var ingredientRecord in ingredientRecords)
            {
                var dict = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(ingredientRecord.Quantity))
                {
                    dict["quantity"] = ingredientRecord.Quantity;
                }
                if (!string.IsNullOrWhiteSpace(ingredientRecord.Part))
                {
                    dict["part"] = ingredientRecord.Part;
                }
                if (!string.IsNullOrWhiteSpace(ingredientRecord.Type))
                {
                    dict["type"] = ingredientRecord.Type;
                }
                items.Add(dict);
            }
            return JsonConvert.SerializeObject(items);
        }
    }

    public class NutritionInfoRecordListConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var nutritionInfoRecords = new List<NutritionInfoRecord>();
            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(text);
            foreach (var item in items)
            {
                var nutritionInfoRecord = new NutritionInfoRecord();
                if (item.ContainsKey("name"))
                {
                    nutritionInfoRecord.Name = item["name"].ToString();
                }
                if (item.ContainsKey("amount"))
                {
                    nutritionInfoRecord.Amount = decimal.Parse(item["amount"].ToString());
                }
                if (item.ContainsKey("unit_of_measurement"))
                {
                    nutritionInfoRecord.UnitOfMeasurement = item["unit_of_measurement"].ToString();
                }
                if (item.ContainsKey("percent_of_daily_value"))
                {
                    nutritionInfoRecord.PercentOfDailyValue = decimal.Parse(item["percent_of_daily_value"].ToString());
                }
                nutritionInfoRecords.Add(nutritionInfoRecord);
            }
            return nutritionInfoRecords;
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            var nutritionInfoRecords = value as List<NutritionInfoRecord>;
            if (nutritionInfoRecords == null) return null;
            var items = new List<Dictionary<string, object>>();
            foreach (var nutritionInfoRecord in nutritionInfoRecords)
            {
                var dict = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(nutritionInfoRecord.Name))
                {
                    dict["name"] = nutritionInfoRecord.Name;
                }
                if (nutritionInfoRecord.Amount != null)
                {
                    dict["amount"] = nutritionInfoRecord.Amount;
                }
                if (!string.IsNullOrWhiteSpace(nutritionInfoRecord.UnitOfMeasurement))
                {
                    dict["unit_of_measurement"] = nutritionInfoRecord.UnitOfMeasurement;
                }
                if (nutritionInfoRecord.PercentOfDailyValue != null)
                {
                    dict["percent_of_daily_value"] = nutritionInfoRecord.PercentOfDailyValue;
                }
                items.Add(dict);
            }
            return JsonConvert.SerializeObject(items);
        }
    }
}