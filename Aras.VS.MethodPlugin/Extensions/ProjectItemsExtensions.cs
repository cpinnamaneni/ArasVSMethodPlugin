﻿//------------------------------------------------------------------------------
// <copyright file="ProjectItemsExtensions.cs" company="Aras Corporation">
//     © 2017-2018 Aras Corporation. All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;

namespace Aras.VS.MethodPlugin.Extensions
{
	internal static class ProjectItemsExtensions
	{
		internal static bool Exists(this ProjectItems projectItems, string fileName)
		{
			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.Name.Equals(fileName))
				{
					return true;
				}
			}

			return false;
		}
	}
}
