using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CardCreator.Editor
{
    // Naming rules and validation with cross check with reserved names.
    public static class CardNaming
    {
        private static readonly Regex _identifierRegex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$");

        // Keywords that could break the code
        private static readonly HashSet<string> _reservedNames = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
            "name", "hideFlags", "data",
            "frontColor", "frontSprite", "backColor", "backSprite"
        };

        public static bool IsValidIdentifier(string pName)
        {
            return !string.IsNullOrEmpty(pName) && _identifierRegex.IsMatch(pName) && !_reservedNames.Contains(pName);
        }

        // Erors on invalid variable names, null if valid.
        public static string GetErrorInName(CardTemplate pTemplate, CardVariable pVariable)
        {
            if (_reservedNames.Contains(pVariable.name))
                return "You can't use this name, pick something else";

            if (!IsValidIdentifier(pVariable.name))
                return "You can only use letters and numbers and underscores. (You can only start with leters)";

            foreach (CardVariable lOther in pTemplate.variables)
            {
                if (lOther != pVariable && lOther.name == pVariable.name)
                    return "Two variables can't have the same name";
            }

            if (pVariable.type == CardVariableType.Enum)
            {
                if (pVariable.enumOptions.Count == 0)
                    return "Enums must have at lest one item";

                HashSet<string> lOptions = new HashSet<string>();

                foreach (string lOption in pVariable.enumOptions)
                {
                    if (!IsValidIdentifier(lOption) || !lOptions.Add(lOption))
                        return $"Enum option \"{lOption}\" is invalid or duplicated.";
                }
            }

            return null;
        }

        // Returns first error found in the template, null if safe to save
        public static string ValidateCardName(CardTemplate pTemplate)
        {
            if (!IsValidIdentifier(pTemplate.templateName))
                return $"\"{pTemplate.templateName}\" isn't a valid card name: You can only use letters and numbers and underscores. (You can only start with letters)";

            foreach (CardVariable lVariable in pTemplate.variables)
            {
                string lError = GetErrorInName(pTemplate, lVariable);

                if (lError != null)
                    return $"Variable \"{lVariable.name}\": {lError}";
            }

            return null;
        }

        public static string GetUniqueVariableName(CardTemplate pTemplate, string pBaseName)
        {
            string lName = pBaseName;
            int lSuffix = 2;

            while (pTemplate.variables.Exists(pVariable => pVariable.name == lName))
                lName = pBaseName + lSuffix++;

            return lName;
        }
    }
}
