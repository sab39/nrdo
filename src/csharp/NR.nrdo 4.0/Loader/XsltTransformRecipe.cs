using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.IO;

namespace NR.nrdo.Loader
{
    class XsltTransformRecipe : ITransformRecipe
    {
        public XmlDocument Transform(XmlDocument recipe)
        {
            using (XmlReader reader = new XmlNodeReader(recipe))
            {
                XmlDocument result = new XmlDocument();
                using (XmlWriter writer = result.CreateNavigator().AppendChild())
                {
                    XsltTransform.Transform(reader, writer);
                    return result;
                }
            }
        }

        private string sourceFile;
        public string SourceFile { get { return sourceFile; } set { sourceFile = value; } }

        private XslCompiledTransform xsltTransform;
        internal XslCompiledTransform XsltTransform
        {
            get
            {
                if (xsltTransform == null)
                {
                    if (SourceFile == null) throw new ArgumentException("src is required on xslt transforms");
                    xsltTransform = new XslCompiledTransform();
                    xsltTransform.Load(SourceFile);
                }
                return xsltTransform;
            }
        }
    }
}
