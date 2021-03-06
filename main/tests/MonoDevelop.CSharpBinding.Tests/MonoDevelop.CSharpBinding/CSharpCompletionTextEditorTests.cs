﻿//
// CSharpCompletionTextEditorTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class CSharpCompletionTextEditorTests : UnitTests.TestBase
	{
		[Test]
		public async Task TestBug58473 ()
		{
			var text = @"$";
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			var project = Ide.Services.ProjectService.CreateDotNetProject ("C#");
			project.Name = "test";
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("mscorlib"));
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("System.Core"));

			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile ("/a.cs", BuildAction.Compile));

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);

			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";
			content.Project = project;


			content.Text = text;
			content.CursorPosition = Math.Max (0, endPos);
			var doc = new MonoDevelop.Ide.Gui.Document (tww);
			doc.SetProject (project);

			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			await doc.UpdateParseDocument ();

			var ctx = new CodeCompletionContext ();

			var tmp = IdeApp.Preferences.EnableAutoCodeCompletion;
			IdeApp.Preferences.EnableAutoCodeCompletion.Set (false);
			var list = await compExt.HandleCodeCompletionAsync (ctx, CompletionTriggerInfo.CodeCompletionCommand);
			Assert.IsNotNull (list);
			IdeApp.Preferences.EnableAutoCodeCompletion.Set (tmp);
			project.Dispose ();
		}

	}
}
