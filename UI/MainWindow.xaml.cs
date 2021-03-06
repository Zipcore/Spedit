﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Controls;
using Xceed.Wpf.AvalonDock.Layout;
using Spedit.UI.Components;
using System.IO;
using Spedit.UI.Windows;

namespace Spedit.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public List<EditorElement> EditorsReferences = new List<EditorElement>();

        Storyboard BlendOverEffect;
        Storyboard FadeFindReplaceGridIn;
        Storyboard FadeFindReplaceGridOut;
        public MainWindow()
        {
            InitializeComponent();
        }
        public MainWindow(SplashScreen sc)
        {
            InitializeComponent();
            FillConfigMenu();
            this.MetroDialogOptions.AnimateHide = this.MetroDialogOptions.AnimateShow = false;
            BlendOverEffect = (Storyboard)this.Resources["BlendOverEffect"];
            FadeFindReplaceGridIn = (Storyboard)this.Resources["FadeFindReplaceGridIn"];
            FadeFindReplaceGridOut = (Storyboard)this.Resources["FadeFindReplaceGridOut"];
#if DEBUG
            TryLoadSourceFile(@"C:\Users\Jelle\Desktop\scripting\AeroControler.sp", false);
#endif
            if (Program.OptionsObject.LastOpenFiles != null)
            {
                for (int i = 0; i < Program.OptionsObject.LastOpenFiles.Length; ++i)
                {
                    TryLoadSourceFile(Program.OptionsObject.LastOpenFiles[i], false);
                }
            }
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                if (!args[i].EndsWith("exe"))
                {
                    TryLoadSourceFile(args[i], false);
                }
            }
            sc.Close(TimeSpan.FromMilliseconds(500.0));
        }

        public bool TryLoadSourceFile(string filePath, bool UseBlendoverEffect = true, bool TryOpenIncludes = true)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                string finalPath = fileInfo.FullName;
                for (int i = 0; i < DockingPane.Children.Count; ++i)
                {
                    if (DockingPane.Children[i].Content is EditorElement)
                    {
                        if (((EditorElement)DockingPane.Children[i].Content).FullFilePath == finalPath)
                        {
                            return false;
                        }
                    }
                }
                AddEditorElement(finalPath, fileInfo.Name);
                if (TryOpenIncludes && Program.OptionsObject.Program_OpenCustomIncludes)
                {
                    using (var textReader = fileInfo.OpenText())
                    {
                        string source = Regex.Replace(textReader.ReadToEnd(), @"/\*.*?\*/", string.Empty, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                        Regex regex = new Regex(@"^\s*\#include\s+((\<|"")(?<name>.+?)(\>|""))", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                        MatchCollection mc = regex.Matches(source);
                        for (int i = 0; i < mc.Count; ++i)
                        {
                            try
                            {
                                string fileName = mc[i].Groups["name"].Value;
                                if (!(fileName.EndsWith(".inc", StringComparison.InvariantCultureIgnoreCase) || fileName.EndsWith(".sp", StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    fileName = fileName + ".inc";
                                }
                                fileName = System.IO.Path.Combine(fileInfo.DirectoryName, fileName);
                                TryLoadSourceFile(fileName, false, Program.OptionsObject.Program_OpenIncludesRecursively);
                            }
                            catch (Exception) { }
                        }
                    }
                }
                if (UseBlendoverEffect)
                {
                    BlendOverEffect.Begin();
                }
                return true;
            }
            return false;
        }

        public void AddEditorElement(string filePath, string name)
        {
            LayoutDocument layoutDocument = new LayoutDocument();
            layoutDocument.Title = name;
            layoutDocument.Closing += layoutDocument_Closing;
            EditorElement editor = new EditorElement(filePath);
            editor.Parent = layoutDocument;
            layoutDocument.Content = editor;
            EditorsReferences.Add(editor);
            DockingPane.Children.Add(layoutDocument);
            DockingPane.SelectedContentIndex = DockingPane.ChildrenCount - 1;
        }

        private void layoutDocument_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((EditorElement)((LayoutDocument)sender).Content).Close();
            e.Cancel = true;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<string> lastOpenFiles = new List<string>();
            EditorElement[] editors = GetAllEditorElements();
            for (int i = 0; i < editors.Length; ++i)
            {
                if (File.Exists(editors[i].FullFilePath))
                {
                    lastOpenFiles.Add(editors[i].FullFilePath);
                }
            }
            Program.OptionsObject.LastOpenFiles = lastOpenFiles.ToArray();
            
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                for (int i = 0; i < files.Length; ++i)
                {
                    TryLoadSourceFile(files[i], (i == 0) ? true : false);
                }
            }
        }

        public static void ProcessUITasks()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }
    }
}
