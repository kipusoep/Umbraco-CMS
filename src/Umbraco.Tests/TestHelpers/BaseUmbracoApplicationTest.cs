﻿using System.Reflection;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.ObjectResolution;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.UnitOfWork;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Umbraco.Tests.TestHelpers
{
    /// <summary>
    /// A base test class used for umbraco tests whcih sets up the logging, plugin manager any base resolvers, etc... and
    /// ensures everything is torn down properly.
    /// </summary>
    [TestFixture]
    public abstract class BaseUmbracoApplicationTest
    {
        [SetUp]
        public virtual void Initialize()
        {
            TestHelper.SetupLog4NetForTests();
            TestHelper.InitializeContentDirectories();
            TestHelper.EnsureUmbracoSettingsConfig();

            SettingsForTests.UseLegacyXmlSchema = false;
            SettingsForTests.ForceSafeAliases = true;
            SettingsForTests.UmbracoLibraryCacheDuration = 1800;
            
            SetupPluginManager();

            SetupApplicationContext();

            FreezeResolution();
        }

        [TearDown]
        public virtual void TearDown()
        {
            //reset settings
            SettingsForTests.Reset();
            UmbracoContext.Current = null;
            TestHelper.CleanContentDirectories();
            TestHelper.CleanUmbracoSettingsConfig();
            //reset the app context, this should reset most things that require resetting like ALL resolvers
            ApplicationContext.Current.DisposeIfDisposable();
            ApplicationContext.Current = null;
            ResetPluginManager();
        }
        
        /// <summary>
        /// By default this returns false which means the plugin manager will not be reset so it doesn't need to re-scan 
        /// all of the assemblies. Inheritors can override this if plugin manager resetting is required, generally needs
        /// to be set to true if the  SetupPluginManager has been overridden.
        /// </summary>
        protected virtual bool PluginManagerResetRequired
        {
            get { return false; }
        }

        /// <summary>
        /// Inheritors can resset the plugin manager if they choose to on teardown
        /// </summary>
        protected virtual void ResetPluginManager()
        {
            if (PluginManagerResetRequired)
            {
                PluginManager.Current = null;    
            }
        }

        /// <summary>
        /// Inheritors can override this if they wish to create a custom application context
        /// </summary>
        protected virtual void SetupApplicationContext()
        {
            //disable cache
            var cacheHelper = CacheHelper.CreateDisabledCacheHelper();

            ApplicationContext.Current = new ApplicationContext(
                //assign the db context
                new DatabaseContext(new DefaultDatabaseFactory()),
                //assign the service context
                new ServiceContext(new PetaPocoUnitOfWorkProvider(), new FileUnitOfWorkProvider(), new PublishingStrategy(), cacheHelper),
                cacheHelper)
            {
                IsReady = true
            };
        }

        /// <summary>
        /// Inheritors can override this if they wish to setup the plugin manager differenty (i.e. specify certain assemblies to load)
        /// </summary>
        protected virtual void SetupPluginManager()
        {
            if (PluginManager.Current == null || PluginManagerResetRequired)
            {
                PluginManager.Current = new PluginManager(false);
                PluginManager.Current.AssembliesToScan = new[]
                {
                    Assembly.Load("Umbraco.Core"),
                    Assembly.Load("umbraco"),
                    Assembly.Load("Umbraco.Tests"),
                    Assembly.Load("businesslogic"),
                    Assembly.Load("cms"),
                    Assembly.Load("controls"),
                    Assembly.Load("umbraco.editorControls"),
                    Assembly.Load("umbraco.MacroEngines"),
                    Assembly.Load("umbraco.providers"),
                    Assembly.Load("Umbraco.Web.UI"),
                };
            }
        }

        /// <summary>
        /// Inheritors can override this to setup any resolvers before resolution is frozen
        /// </summary>
        protected virtual void FreezeResolution()
        {
            Resolution.Freeze();
        }

        protected ApplicationContext ApplicationContext
        {
            get { return ApplicationContext.Current; }
        }
    }
}