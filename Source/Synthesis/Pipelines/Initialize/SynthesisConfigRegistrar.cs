﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Synthesis.Configuration;
using Synthesis.Configuration.Registration;
using Synthesis.Utility;

namespace Synthesis.Pipelines.Initialize
{
	/// <summary>
	/// Registers all Synthesis configurations defined in a specific set of assemblies.
	/// To register the Synthesis default configuration, load configurations in the Synthesis assembly.
	/// </summary>
	public class SynthesisConfigRegistrar
	{
		protected readonly List<Assembly> Assemblies = new List<Assembly>();

		public virtual void Process(PipelineArgs args)
		{
            /*while(!Debugger.IsAttached)
            {
                System.Threading.Thread.Sleep(1000);
            }*/
			var types = GetTypesInRegisteredAssemblies();
			var configurations = GetConfigurationsFromTypes(types);

			foreach (var configRegistration in configurations)
			{
				ProviderResolver.RegisterConfiguration(configRegistration.GetConfiguration());
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ignore all load errors on assembly types")]
		protected virtual IEnumerable<Type> GetTypesInRegisteredAssemblies()
		{
			if (Assemblies.Count == 0) throw new InvalidOperationException("You must specify the assemblies to scan for Synthesis configurations, e.g. <assemblies hint=\"list: AddAssembly\"><default>Synthesis</default></assemblies>");

			IEnumerable<Assembly> assemblies = Assemblies;

			return assemblies.SelectMany(delegate (Assembly x)
			{
				try { return x.GetExportedTypes(); }
				catch (ReflectionTypeLoadException rex) { return rex.Types.Where(y => y != null).ToArray(); } // http://haacked.com/archive/2012/07/23/get-all-types-in-an-assembly.aspx
							catch { return new Type[] { }; }
			}).ToList();
		}

		protected virtual IEnumerable<ISynthesisConfigurationRegistration> GetConfigurationsFromTypes(IEnumerable<Type> types)
		{
			return types
				.Where(type => typeof (ISynthesisConfigurationRegistration).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
				.Select(Activator.CreateInstance)
				.OfType<ISynthesisConfigurationRegistration>()
				.ToArray();
		}

		public virtual void AddAssembly(string name)
		{
			// ignore assemblies already added
			if (Assemblies.Any(existing => existing.GetName().Name.Equals(name, StringComparison.Ordinal))) return;

			if (name.Contains("*"))
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var assembly in assemblies)
				{
					var assemblyName = assembly.GetName().Name;
					if (WildcardUtility.IsWildcardMatch(assemblyName, name)) AddAssembly(assemblyName);
				}

				return;
			}

			Assembly a = Assembly.Load(name);
			if (a == null) throw new ArgumentException("The assembly name was not valid");

			Assemblies.Add(a);
		}
	}
}
