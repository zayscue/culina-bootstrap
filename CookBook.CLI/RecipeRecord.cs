using System.Collections.Generic;

namespace Culina.Bootstrap.CookBook.CLI
{
    public class RecipeRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Minutes { get; set; }
        public int ContributorId { get; set; }
        public string Submitted { get; set; }
        public List<string> Tags { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Serves { get; set; }
        public string Yield { get; set; }
        public string ServingSize { get; set; }
        public int ServingsPerRecipe { get; set; }
        public int NumberOfSteps { get; set; }
        public List<string> Steps { get; set; }
        public List<IngredientRecord> Ingredients { get; set; }
        public int NumberOfIngredients { get; set; }
        public List<NutritionInfoRecord> NutritionInfo { get; set; }
    }

    public class IngredientRecord
    {
        public string Quantity { get; set; }
        public string Part { get; set; }
        public string Type { get; set; }
    }

    public class NutritionInfoRecord
    {
        public string Name { get; set; }
        public decimal? Amount { get; set; }
        public string UnitOfMeasurement { get; set; }
        public decimal? PercentOfDailyValue { get; set; }
    }
}