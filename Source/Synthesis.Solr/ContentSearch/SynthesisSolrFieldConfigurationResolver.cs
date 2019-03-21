using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.FieldNames;
using Sitecore.ContentSearch.SolrProvider.FieldNames.TypeResolving;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using System;

namespace Synthesis.Solr.ContentSearch
{
    public class SynthesisSolrFieldConfigurationResolver : SolrFieldConfigurationResolver
    {
        private readonly TemplateFieldTypeResolver fieldTypeResolver;

        private readonly SolrFieldMap solrFieldMap;

        private readonly SolrIndexSchema solrSchema;

        public SynthesisSolrFieldConfigurationResolver(SolrFieldMap solrFieldMap, SolrIndexSchema solrSchema, TemplateFieldTypeResolver fieldTypeResolver) :
            base(solrFieldMap, solrSchema, fieldTypeResolver)
        {
            Assert.ArgumentNotNull(fieldTypeResolver, "fieldTypeResolver");
            Assert.IsNotNull(solrFieldMap, "solrFieldMap");
            Assert.IsNotNull(solrSchema, "solrSchema");
            this.fieldTypeResolver = fieldTypeResolver;
            this.solrFieldMap = solrFieldMap;
            this.solrSchema = solrSchema;
        }

        public override SolrSearchFieldConfiguration Resolve(string fieldName, string normalizedFieldName, string returnTypeString, string fieldType, Type valueType, bool resolveByName, bool resolveByTemplateField)
        {
            var config = this.TryResolveFieldConfigurationByName(normalizedFieldName, resolveByName);
            if (config == null)
                config = this.ResolveFieldConfigurationByReturnType(returnTypeString);
            if (config == null)
                config = this.ResolveFieldConfigurationByFieldType(fieldType);
            if (config == null)
                config = this.ResolveFieldConfigurationByValueType(valueType);
            if (config == null)
                config = this.TryAggressiveResolve(fieldName, normalizedFieldName, resolveByTemplateField);
            return config;
        }

        private SolrSearchFieldConfiguration ResolveFieldConfigurationByFieldType(string fieldType)
        {
            if (fieldType.IsNullOrEmpty())
            {
                return null;
            }
            return this.solrFieldMap.GetFieldConfigurationByFieldTypeName(fieldType) as SolrSearchFieldConfiguration;
        }

        private SolrSearchFieldConfiguration ResolveFieldConfigurationByName(string fieldName)
        {
            SolrSearchFieldConfiguration fieldConfiguration = this.solrFieldMap.GetFieldConfiguration(fieldName) as SolrSearchFieldConfiguration;
            if (fieldConfiguration == null)
            {
                if (!this.solrSchema.IsSchemaField(fieldName))
                {
                    return null;
                }
                fieldConfiguration = new SynthesisSolrFieldConfigurationResolver.InSchemaSolrSearchFieldConfiguration();
            }
            return fieldConfiguration;
        }

        private SolrSearchFieldConfiguration ResolveFieldConfigurationByReturnType(string returnType)
        {
            if (returnType.IsNullOrEmpty())
            {
                return null;
            }
            return this.solrFieldMap.GetFieldConfigurationByReturnType(returnType) as SolrSearchFieldConfiguration;
        }

        private SolrSearchFieldConfiguration ResolveFieldConfigurationByTemplateField(string fieldName, string normalizedFieldName)
        {
            string str = this.fieldTypeResolver.ResolveType(fieldName);
            if (str.IsNullOrEmpty())
            {
                return null;
            }
            SolrSearchFieldConfiguration fieldConfigurationByFieldTypeName = this.solrFieldMap.GetFieldConfigurationByFieldTypeName(str) as SolrSearchFieldConfiguration;
            if (fieldConfigurationByFieldTypeName != null)
            {
                this.solrFieldMap.AddFieldByFieldName(normalizedFieldName, fieldConfigurationByFieldTypeName);
            }
            return fieldConfigurationByFieldTypeName;
        }

        private SolrSearchFieldConfiguration ResolveFieldConfigurationByValueType(Type valueType)
        {
            if (valueType == null)
            {
                return null;
            }
            return this.solrFieldMap.GetFieldConfiguration(valueType) as SolrSearchFieldConfiguration;
        }

        private SolrSearchFieldConfiguration TryAggressiveResolve(string fieldName, string normalizedFieldName, bool resolveByTemplateField)
        {
            if (!resolveByTemplateField)
            {
                return null;
            }
            return this.ResolveFieldConfigurationByTemplateField(fieldName, normalizedFieldName);
        }

        private SolrSearchFieldConfiguration TryResolveFieldConfigurationByName(string fieldName, bool resolveByName)
        {
            if (!resolveByName)
            {
                return null;
            }
            return this.ResolveFieldConfigurationByName(fieldName);
        }

        private sealed class InSchemaSolrSearchFieldConfiguration : SolrSearchFieldConfiguration
        {
            public InSchemaSolrSearchFieldConfiguration()
            {
            }

            public override string FormatFieldName(string fieldName, ISearchIndexSchema schema, string cultureCode, string defaultCulture)
            {
                return fieldName;
            }
        }
    }
}