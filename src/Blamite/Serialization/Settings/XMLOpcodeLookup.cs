﻿using System.Linq;
using System.Xml.Linq;
using Blamite.Blam.Scripting;
using Blamite.Util;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blamite.Serialization.Settings
{
	/// <summary>
	///     Loads script opcode lookup data from XML files.
	/// </summary>
	public class XMLOpcodeLookupLoader : IComplexSettingLoader
	{
		/// <summary>
		///     Loads setting data from a path.
		/// </summary>
		/// <param name="path">The path to load from.</param>
		/// <returns>
		///     The loaded setting data.
		/// </returns>
		public object LoadSetting(string path)
		{
			XDocument document = XDocument.Load(path);
			var result = new OpcodeLookup();
			XElement root = document.Element("BlamScript");

			RegisterExecutionTypes(root, result);
			RegisterValueTypes(root, result);
			RegisterFunctions(root, result);
            RegisterGlobals(root, result);
            RegisterTypeCasts(root, result);

			return result;
		}

		private void RegisterExecutionTypes(XContainer root, OpcodeLookup lookup)
		{
			foreach (XElement element in root.Element("scriptTypes").Descendants("type"))
			{
				var opcode = (ushort) XMLUtil.GetNumericAttribute(element, "opcode");
				string name = XMLUtil.GetStringAttribute(element, "name");
				lookup.RegisterScriptType(name, opcode);
			}
		}

		private void RegisterValueTypes(XContainer root, OpcodeLookup lookup)
		{
			foreach (XElement element in root.Element("valueTypes").Descendants("type"))
			{
				string name = XMLUtil.GetStringAttribute(element, "name");
				var opcode = (ushort) XMLUtil.GetNumericAttribute(element, "opcode");
				int size = (int)XMLUtil.GetNumericAttribute(element, "size");
				bool quoted = XMLUtil.GetBoolAttribute(element, "quoted", false);
				string tag = XMLUtil.GetStringAttribute(element, "tag", null);
				bool obj = XMLUtil.GetBoolAttribute(element, "object", false);
				var valueType = new ScriptValueType(name, opcode, size, quoted, tag, obj);
                foreach(XElement option in element.Descendants("enum"))
                {
                    valueType.AddEnumValue(option.Value);
                }
				lookup.RegisterValueType(valueType);
			}
		}

		private void RegisterFunctions(XContainer root, OpcodeLookup lookup)
		{
			foreach (XElement element in root.Element("functions").Descendants("function"))
			{
				string name = XMLUtil.GetStringAttribute(element, "name");
				if (name == "")
				{
					continue;

				}

				var opcode = (ushort) XMLUtil.GetNumericAttribute(element, "opcode");
				string returnType = XMLUtil.GetStringAttribute(element, "returnType", "void");
				var flags = (uint) XMLUtil.GetNumericAttribute(element, "flags", 0);
                string group = XMLUtil.GetStringAttribute(element, "group", null);
				bool isNull = XMLUtil.GetBoolAttribute(element, "null", false);
				string[] parameterTypes = element.Descendants("arg").Select(e => XMLUtil.GetStringAttribute(e, "type")).ToArray();

				var info = new FunctionInfo(name, opcode, returnType, flags, group, parameterTypes, !isNull);
				lookup.RegisterFunction(info);
			}
		}

        private void RegisterGlobals(XContainer root, OpcodeLookup lookup)
        {
            foreach (XElement element in root.Element("globals").Descendants("global"))
            {
                string name = XMLUtil.GetStringAttribute(element, "name");
                if (name == "")
				{
					continue;
				}

				ushort opcode = (ushort)XMLUtil.GetNumericAttribute(element, "opcode");
                string returnType = XMLUtil.GetStringAttribute(element, "type");
				bool isNull = XMLUtil.GetBoolAttribute(element, "null", false);

				var info = new GlobalInfo(name, opcode, returnType, !isNull);
                lookup.RegisterGlobal(info);
            }
        }

        private void RegisterTypeCasts(XContainer root, OpcodeLookup lookup)
        {
            foreach (XElement element in root.Element("typecasting").Elements("to"))
            {
                string to = XMLUtil.GetStringAttribute(element, "name");
                bool castOnly = XMLUtil.GetBoolAttribute(element, "castOnly", false);
                List<string> from = element.Elements("from").Select(e => e.Value).ToList();
                CastInfo info = new CastInfo(to, castOnly, from);
                lookup.RegisterTypeCast(to, info);
            }
        }

    }
}