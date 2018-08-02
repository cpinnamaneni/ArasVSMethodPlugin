﻿//------------------------------------------------------------------------------
// <copyright file="OpenFromArasCmd.cs" company="Aras Corporation">
//     © 2017-2018 Aras Corporation. All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Linq;
using Aras.VS.MethodPlugin.Authentication;
using Aras.VS.MethodPlugin.Code;
using Aras.VS.MethodPlugin.Dialogs;
using Aras.VS.MethodPlugin.Dialogs.Views;
using Aras.VS.MethodPlugin.PackageManagement;
using Aras.VS.MethodPlugin.ProjectConfigurations;
using Aras.VS.MethodPlugin.SolutionManagement;
using Aras.VS.MethodPlugin.Templates;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace Aras.VS.MethodPlugin.Commands
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class OpenFromArasCmd : AuthenticationCommandBase
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x101;

		public const int ToolbarCommandId = 0x102;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("AEA8535B-C666-4112-9BDD-5ECFA4934B47");

		public static readonly Guid ToolbarCommandSet = new Guid("21D122E1-35BF-4156-B458-7E292CDD9C2D");

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenFromArasCmd"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public OpenFromArasCmd(IProjectManager projectManager, IAuthenticationManager authManager, IDialogFactory dialogFactory, ProjectConfigurationManager projectConfigurationManager, ICodeProviderFactory codeProviderFactory) : base(authManager, dialogFactory, projectManager, projectConfigurationManager, codeProviderFactory)
		{
			if (projectManager.CommandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new MenuCommand(this.ExecuteCommand, menuCommandID);
				var toolbarMenuCommandID = new CommandID(ToolbarCommandSet, ToolbarCommandId);
				var toolbarMenuItem = new MenuCommand(this.ExecuteCommand, toolbarMenuCommandID);

				projectManager.CommandService.AddCommand(menuItem);
				projectManager.CommandService.AddCommand(toolbarMenuItem);
			}
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static OpenFromArasCmd Instance
		{
			get;
			private set;
		}

		public static void Initialize(IProjectManager projectManager, IAuthenticationManager authManager, IDialogFactory dialogFactory, ProjectConfigurationManager projectConfigurationManager, ICodeProviderFactory codeProviderFactory)
		{
			Instance = new OpenFromArasCmd(projectManager, authManager, dialogFactory, projectConfigurationManager, codeProviderFactory);
		}

		public override void ExecuteCommandImpl(object sender, EventArgs args, IVsUIShell uiShell)
		{
			var project = projectManager.SelectedProject;
			string projectConfigPath = projectManager.ProjectConfigPath;

			ProjectConfiguraiton projectConfiguration = projectConfigurationManager.Load(projectConfigPath);

			ICodeProvider codeProvider = codeProviderFactory.GetCodeProvider(project.CodeModel.Language, projectConfiguration);

			var templateLoader = new TemplateLoader();
			templateLoader.Load(projectManager.MethodConfigPath);

			var packageManager = new PackageManager(authManager);
			var openView = dialogFactory.GetOpenFromArasView(projectManager.UIShell, projectConfigurationManager, projectConfiguration, templateLoader, packageManager, projectConfigPath, project.Name, project.FullName, codeProvider.Language);

			var openViewResult = openView.ShowDialog();
			if (openViewResult?.DialogOperationResult != true)
			{
				return;
			}

			MethodInfo methodInformation = projectConfiguration.MethodInfos.FirstOrDefault(m => m.MethodName == openViewResult.MethodName);
			bool isMethodExist = projectManager.IsMethodExist(openViewResult.MethodName);
			if (projectManager.IsMethodExist(openViewResult.MethodName))
			{
				var messageWindow = new MessageBoxWindow();
				var dialogReuslt = messageWindow.ShowDialog(null,
					"Method already added to project. Do you want replace method?",
					"Warning",
					MessageButtons.YesNo,
					MessageIcon.None);

				if (dialogReuslt == MessageDialogResult.Yes)
				{
					projectManager.RemoveMethod(methodInformation);
					projectConfiguration.MethodInfos.Remove(methodInformation);
				}
				else
				{
					return;
				}
			}

			GeneratedCodeInfo codeInfo = codeProvider.GenerateCodeInfo(openViewResult.SelectedTemplate, openViewResult.SelectedEventSpecificData, openViewResult.MethodName, false, openViewResult.MethodCode);
			projectManager.CreateMethodTree(codeInfo);

			var methodInfo = new MethodInfo()
			{
				InnovatorMethodConfigId = openViewResult.MethodConfigId,
				InnovatorMethodId = openViewResult.MethodId,
				MethodLanguage = openViewResult.MethodLanguage,
				MethodName = openViewResult.MethodName,
				MethodType = openViewResult.MethodType ?? "server",
				MethodComment = openViewResult.MethodComment,
				PackageName = openViewResult.Package,
				TemplateName = openViewResult.SelectedTemplate.TemplateName,
				EventData = openViewResult.SelectedEventSpecificData.EventSpecificData,
				ExecutionAllowedToId = openViewResult.SelectedIdentityId,
				ExecutionAllowedToKeyedName = openViewResult.SelectedIdentityKeyedName,
				PartialClasses = codeInfo.PartialCodeInfoList.Select(pci => pci.Path).ToList()
			};

			projectConfiguration.AddMethodInfo(methodInfo);
			projectConfigurationManager.Save(projectConfigPath, projectConfiguration);
		}
	}
}
