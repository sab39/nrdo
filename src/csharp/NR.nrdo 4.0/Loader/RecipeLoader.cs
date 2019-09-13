using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using NR.nrdo.Reflection;

namespace NR.nrdo.Loader
{
    public class RecipeLoader
    {
        public RecipeLoader()
            : this(Assembly.GetCallingAssembly()) { }

        public RecipeLoader(Assembly assembly)
            : this(NrdoReflection.GetLookupStrategy(assembly)) { }

        public RecipeLoader(ILookupAssemblies lookup)
            : this(new RecipeContext(lookup)) { }

        public RecipeLoader(RecipeContext context)
        {
            Context = context;
        }

        public void LoadRecipe(Stream stream)
        {
            recipes.Add(new Recipe(stream, Context));
        }
        public void LoadRecipe(FileInfo file)
        {
            LoadRecipe(file.OpenRead());
        }
        public void LoadRecipe(string path)
        {
            LoadRecipe(new FileInfo(path));
        }

        public void RunRecipe(Stream stream)
        {
            new Recipe(stream, Context).Run(Context);
        }
        public void RunRecipe(FileInfo file)
        {
            RunRecipe(file.OpenRead());
        }
        public void RunRecipe(string path)
        {
            RunRecipe(new FileInfo(path));
        }

        public RecipeContext Context { get; set; }

        private List<Recipe> recipes = new List<Recipe>();
        public void RunAll()
        {
            List<Recipe> recipes = new List<Recipe>(this.recipes);
            foreach (Recipe recipe in recipes)
            {
                recipe.Run(Context);
                this.recipes.Remove(recipe);
            }
        }
    }
}
