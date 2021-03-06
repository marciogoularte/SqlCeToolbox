﻿using System.Windows.Controls;
using System.Windows.Input;
using ErikEJ.SqlCeToolbox.Commands;
using ErikEJ.SqlCeToolbox.Helpers;
using ErikEJ.SqlCeToolbox.ToolWindows;

namespace ErikEJ.SqlCeToolbox.ContextMenues
{

    public class IndexContextMenu : ContextMenu
    {

        public IndexContextMenu(MenuCommandParameters menuCommandParameters, ExplorerControl parent)
        {
            var tcmd = new IndexMenuCommandsHandler(parent);
            CreateScriptAsCreateMenuItem(tcmd, menuCommandParameters);
            CreateScriptAsDropMenuItem(tcmd, menuCommandParameters);
            Items.Add(new Separator());
            CreateScriptAsStatisticsMenuItem(tcmd, menuCommandParameters);
            
        }

        private void CreateScriptAsCreateMenuItem(IndexMenuCommandsHandler tcmd, MenuCommandParameters menuCommandParameters)
        {
            var scriptCommandBinding = new CommandBinding(IndexMenuCommands.IndexCommand, tcmd.ScriptAsCreate);
            var scriptMenuItem = new MenuItem
            {
                Header = "Script as CREATE",
                Icon = ImageHelper.GetImageFromResource("../resources/sp.png"),
                Command = IndexMenuCommands.IndexCommand,
                CommandParameter = menuCommandParameters
            };
            scriptMenuItem.CommandBindings.Add(scriptCommandBinding);
            Items.Add(scriptMenuItem);
        }

        private void CreateScriptAsDropMenuItem(IndexMenuCommandsHandler tcmd, MenuCommandParameters menuCommandParameters)
        {
            var scriptCommandBinding = new CommandBinding(IndexMenuCommands.IndexCommand, tcmd.ScriptAsDrop);
            var scriptMenuItem = new MenuItem
            {
                Header = "Script as DROP",
                Icon = ImageHelper.GetImageFromResource("../resources/sp.png"),
                Command = IndexMenuCommands.IndexCommand,
                CommandParameter = menuCommandParameters
            };
            scriptMenuItem.CommandBindings.Add(scriptCommandBinding);
            Items.Add(scriptMenuItem);
        }

        private void CreateScriptAsStatisticsMenuItem(IndexMenuCommandsHandler tcmd, MenuCommandParameters menuCommandParameters)
        {
            var scriptCommandBinding = new CommandBinding(IndexMenuCommands.IndexCommand, tcmd.ScriptAsStatistics);
            var scriptMenuItem = new MenuItem
            {
                Header = "Script as Statistics",
                Icon = ImageHelper.GetImageFromResource("../resources/sp.png"),
                Command = IndexMenuCommands.IndexCommand,
                CommandParameter = menuCommandParameters
            };
            scriptMenuItem.CommandBindings.Add(scriptCommandBinding);
            Items.Add(scriptMenuItem);
        }


    }
}