#nullable enable

using System;
using System.Linq;
using System.Xml.Linq;

namespace RuStore.Editor.Manifest {

    internal static class XElementExtensions {

        public static XElement? GetXElement(this XElement parent, string elementName, Func<XElement, bool> match) =>
            parent.Elements(elementName).FirstOrDefault(match);

        public static XElement GetOrCreateXElement(this XElement parent, string elementName, Func<XElement, bool> match, Action<XElement>? init = null) {
            var element = parent.GetXElement(elementName, match);
            if (element != null) return element;

            element = new XElement(elementName);
            parent.Add(element);
            init?.Invoke(element);

            return element;
        }

        public static XElement? FindXElementByAttribute(this XElement parent, string elementName, XName attributeName, string attributeValue) =>
            parent.Elements(elementName)
                .FirstOrDefault(e => (string?)e.Attribute(attributeName) == attributeValue);

        public static int RemoveXElement(this XElement parent, string elementName, Func<XElement, bool>? predicate = null) {
            var query = parent.Elements(elementName);

            if (predicate != null) {
                query = query.Where(predicate);
            }

            var toRemove = query.ToList();
            foreach (var el in toRemove) {
                el.Remove();
            }

            return toRemove.Count;
        }
    }
}
