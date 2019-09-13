using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NR.nrdo.Util
{
    public static class XmlUtil
    {
        public static string GetAttr(XmlElement element, string name)
        {
            return element.HasAttribute(name) ? element.GetAttribute(name) : null;
        }
    }
}
