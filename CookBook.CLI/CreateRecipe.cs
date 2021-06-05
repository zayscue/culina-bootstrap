using System.Collections.Generic;

namespace Culina.Bootstrap.CookBook.CLI
{
    public class CreateRecipeCommand
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int EstimatedMinutes { get; set; }
        public string Serves { get; set; }
        public string Yield { get; set; }

        public IList<string> Steps { get; set; }
        public IList<CreateRecipeIngredientCommand> Ingredients { get; set; }

        public IList<string> ImageUrls { get; set; }
        public CreateRecipeNutritionCommand Nutrition { get; set; }
        public IList<CreateRecipeMetadataCommand> Metadata { get; set; }
        public IList<string> Tags { get; set; }
    }

    public class CreateRecipeMetadataCommand
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class CreateRecipeIngredientCommand
    {
        public string Quantity { get; set; }
        public string Part { get; set; }
        public string Type { get; set; }
    }

    public class CreateRecipeNutritionCommand
    {
        public string ServingSize { get; set; }
        public int ServingsPerRecipe { get; set; }
        public decimal Calories { get; set; }
        public decimal CaloriesFromFat { get; set; }
        public decimal CaloriesFromFatPdv { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalFatPdv { get; set; }
        public decimal SaturatedFat { get; set; }
        public decimal SaturatedFatPdv { get; set; }
        public decimal Cholesterol { get; set; }
        public decimal CholesterolPdv { get; set; }
        public decimal DietaryFiber { get; set; }
        public decimal DietaryFiberPdv { get; set; }
        public decimal Sugar { get; set; }
        public decimal SugarPdv { get; set; }
        public decimal Sodium { get; set; }
        public decimal SodiumPdv { get; set; }
        public decimal Protein { get; set; }
        public decimal ProteinPdv { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal TotalCarbohydratesPdv { get; set; }
    }
}