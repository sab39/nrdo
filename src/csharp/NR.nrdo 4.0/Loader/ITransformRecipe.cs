using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NR.nrdo.Loader
{
    public interface ITransformRecipe
    {
        /// <summary>
        /// Perform a transformation on a recipe. The input document is guaranteed to be a valid raw-format recipe; the
        /// output is permitted to be in friendly format.
        /// </summary>
        /// <param name="recipe">The recipe to operate on.</param>
        /// <returns>The transformed document. This may be the same document that was passed in, modified, or it may be an entirely new instance.</returns>
        XmlDocument Transform(XmlDocument recipe);

        /// <summary>
        /// The source document to use for the transformation. For an XSLT transform, for example, this is the URL to the XSLT file.
        /// </summary>
        string SourceFile { get; set;}
    }
}
