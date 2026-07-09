using System;
using System.Collections.Generic;
using System.Text;

namespace SpireChess.Config
{
    public sealed class ConfigValidationResult
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            errors.Add(message);
        }

        public void AddWarning(string message)
        {
            warnings.Add(message);
        }

        public void ThrowIfInvalid()
        {
            if (IsValid)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Config validation failed:");
            foreach (var error in errors)
            {
                builder.AppendLine("- " + error);
            }

            throw new InvalidOperationException(builder.ToString());
        }
    }
}
