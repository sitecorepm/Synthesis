using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Abstractions;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.FieldNames;
using Sitecore.ContentSearch.SolrProvider.FieldNames.Normalization;
using Sitecore.ContentSearch.SolrProvider.FieldNames.TypeResolving;
using SolrNet.Schema;
using Synthesis.ContentSearch;
using Synthesis.FieldTypes.Interfaces;

namespace Synthesis.Solr.ContentSearch
{
    public class SynthesisSolrFieldNameTranslator : SolrFieldNameTranslator
    {

        private readonly SolrSchema _schema;

        public SynthesisSolrFieldNameTranslator(SolrFieldMap solrFieldMap, SolrIndexSchema solrIndexSchema,
            ISettings settings, ISolrFieldConfigurationResolver fieldConfigurationResolver,
            IExtensionStripHelper extensionStripHelper, TemplateFieldTypeResolverFactory typeResolverFactory,
            ICultureContextGuard cultureContextGuard)
            : base(solrFieldMap, solrIndexSchema, settings, fieldConfigurationResolver, extensionStripHelper,
                typeResolverFactory, cultureContextGuard)
        {
            _schema = solrIndexSchema.SolrSchema;
        }


        public override string GetIndexFieldName(string fieldName)
        {
            if (_schema != null && (_schema.FindSolrFieldByName(fieldName) != null || _schema.SolrDynamicFields.Any(x => fieldName.EndsWith(x.Name.Substring(1)))))
                return fieldName;
            //at this point we can't be sure what type the data is in the field, our best bet would be a text field.
            var result = AppendSolrText(fieldName);
            System.Diagnostics.Debug.WriteLine($"M0 {fieldName} ==> {result}");
            return result;
        }

        public override string GetIndexFieldName(MemberInfo member)
        {
            Type fieldType;
            string name = member.Name;
            IIndexFieldNameFormatterAttribute indexFieldNameFormatterAttribute = this.GetIndexFieldNameFormatterAttribute(member);
            if (indexFieldNameFormatterAttribute != null)
            {
                name = indexFieldNameFormatterAttribute.GetIndexFieldName(member.Name);
            }
            if (!(member is PropertyInfo))
            {
                if (!(member is FieldInfo))
                {
                    throw new NotSupportedException(string.Concat("Unexpected member type: ", member.GetType().FullName));
                }
                fieldType = ((FieldInfo)member).FieldType;
            }
            else
            {
                fieldType = ((PropertyInfo)member).PropertyType;
            }

            var result = ProcessFieldName(name, null, fieldType, null, "", false, true);
            System.Diagnostics.Debug.WriteLine($"M1 {member?.Name ?? "null"} ==> {result}");
            return result;
        }

        public override string GetIndexFieldName(string fieldName, Type returnType)
        {
            var result = ProcessFieldName(PreProcessSynthesisFieldName(fieldName), null, returnType, null, "", false, true);
            System.Diagnostics.Debug.WriteLine($"M2 {fieldName} ({returnType.Name}) ==> {result}");
            return result;
        }


        private string ProcessFieldName(string fieldName, string fieldTypeKey, Type returnType, CultureInfo culture, string returnTypeString = "", bool resolveByTemplateField = false, bool resolveByName = true)
        {
            if (typeof(IFieldType).IsAssignableFrom(returnType))
            {
                return fieldName;
            }

            string normalizedFieldName = this.FieldNameNormalizer.NormalizeFieldName(fieldName);
            SolrSearchFieldConfiguration fieldConfiguration = this.FieldConfigurationResolver.Resolve(fieldName, normalizedFieldName, returnTypeString, fieldTypeKey, returnType, resolveByName, resolveByTemplateField);

            //string twoLetterCodeOrDefault = this.cultureContextGuard.GetTwoLetterCodeOrDefault(culture);
            FieldInfo fi = typeof(SolrFieldNameTranslator).GetField("cultureContextGuard", BindingFlags.NonPublic | BindingFlags.Instance);
            var cultureContextGuard = fi.GetValue(this) as ICultureContextGuard;
            string twoLetterCodeOrDefault = cultureContextGuard.GetTwoLetterCodeOrDefault(culture);

            if (fieldConfiguration == null)
            {
                return normalizedFieldName.ToLowerInvariant();
            }
            return fieldConfiguration.FormatFieldName(normalizedFieldName, this.SolrSchema, twoLetterCodeOrDefault, null);
        }

        public override IIndexFieldNameFormatterAttribute GetIndexFieldNameFormatterAttribute(MemberInfo member)
        {
            var result = base.GetIndexFieldNameFormatterAttribute(member);
            System.Diagnostics.Debug.WriteLine($"M3 {member?.Name ?? "null"} ==> {result?.GetIndexFieldName(member?.Name) ?? "null"}");
            return result;
        }

        public override IEnumerable<string> GetTypeFieldNames(string fieldName)
        {
            var result = base.GetTypeFieldNames(fieldName);
            System.Diagnostics.Debug.WriteLine($"M4 {fieldName} ==> {string.Join(",", result)}");
            return result;
        }

        /// <summary>
        /// If the context is a foreign language we should use the foreign language text solr fields
        /// </summary>
        /// <param name="fieldName">the initial field name</param>
        /// <returns>field name with a dynamic field identifier on it</returns>
        private string AppendSolrText(string fieldName)
        {
            if (Context.Site == null || Context.Language.Name == Context.Site.Language)
                fieldName += "_t";
            else
                fieldName += "_t_" + Context.Language;
            return fieldName;
        }

        protected virtual string PreProcessSynthesisFieldName(string fieldName)
        {
            return fieldName.Split('.').First();
        }
    }
}
