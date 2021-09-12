using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Razor;
using Shared;

namespace SplendidBlazor
{
    public class CompileService
    {
        private readonly HttpClient _http;
        private readonly NavigationManager _uriHelper;
        public List<string> CompileLog { get; set; }
        private List<MetadataReference> references { get; set; }

        private BehaviorSubject<Assembly> _dynamicAssembly = new(null);

        public IObservable<Assembly> DynamicAssembly => _dynamicAssembly;

        public CompileService(HttpClient http, NavigationManager uriHelper)
        {
            _http = http;
            _uriHelper = uriHelper;
        }

        public async Task Init(bool reload = false)
        {
            if (references == null)
            {
                references = new List<MetadataReference>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    var name = assembly.GetName().Name + ".dll";
                    Console.WriteLine(name);

                    references.Add(
                        MetadataReference.CreateFromStream(
                            await _http.GetStreamAsync(_uriHelper.BaseUri + "/_framework/" + name)));
                }
            }

            if (DynamicAssembly == null || reload)
            {
                var files = await _http.GetFromJsonAsync<List<RazorFile>>("RazorFile");
                
                CompileLog = new();
                await CompileBlazor(files);
            }
        }

        private async Task CompileBlazor(List<RazorFile> files)
        {
            if (files == null || !files.Any()) return;

            CompileLog.Add("Create fileSystem");

            var fileSystem = new EmptyRazorProjectFileSystem();

            CompileLog.Add("Create engine");
            //            Microsoft.AspNetCore.Blazor.Build.

            var de = RazorConfiguration.Default;
            var x = de.LanguageVersion;
            var engine = RazorProjectEngine.Create(
                RazorConfiguration.Create(RazorLanguageVersion.Version_5_0, "Blazor", new List<RazorExtension>()),
                fileSystem, b =>
                {
                    b.SetRootNamespace("SplendidBlazor");


                    b.Features.Add(new CompilationTagHelperFeature());
                    b.Features.Add(new DefaultMetadataReferenceFeature()
                    {
                        References = references,
                    });

                    b.SetCSharpLanguageVersion(LanguageVersion.Latest);

                    CompilerFeatures.Register(b);
                });

            var csContents = new List<string>();

            foreach (var file in files)
            {
                CompileLog.Add($"Create file {file.ClassName}");
                var fakeFile = new MemoryRazorProjectItem(file.Contents, true, "", $"/{file.ClassName}.razor");
                var doc = engine.Process(fakeFile).GetCSharpDocument();
                CompileLog.Add("Get GeneratedCode");
                var csCode = doc.GeneratedCode;

                CompileLog.Add("Read Diagnostics");
                foreach (var diagnostic in doc.Diagnostics)
                {
                    CompileLog.Add(diagnostic.ToString());
                }

                if (doc.Diagnostics.Any(i => i.Severity == RazorDiagnosticSeverity.Error))
                {
                    continue;
                }

                CompileLog.Add(csCode);
                csContents.Add(csCode);
            }

            CompileLog.Add("Compile assembly");
            var assembly = await Compile(csContents);
            _dynamicAssembly.OnNext(assembly);
        }


        private async Task<Assembly> Compile(List<string> csContents)
        {
            var syntaxTrees = new List<SyntaxTree>();

            foreach (var content in csContents)
            {
                var syntaxTree =
                    CSharpSyntaxTree.ParseText(content, new CSharpParseOptions(LanguageVersion.Preview));
                foreach (var diagnostic in syntaxTree.GetDiagnostics())
                {
                    CompileLog.Add(diagnostic.ToString());
                }

                if (syntaxTree.GetDiagnostics().Any(i => i.Severity == DiagnosticSeverity.Error))
                {
                    CompileLog.Add("Parse SyntaxTree Error!");
                    return null;
                }
                
                syntaxTrees.Add(syntaxTree);
            }


            CompileLog.Add("Parse SyntaxTree Success");

            CSharpCompilation compilation = CSharpCompilation.Create("SplendidBlazor.Dynamic", syntaxTrees,
                references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false));

            using (MemoryStream stream = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stream);

                foreach (var diagnostic in result.Diagnostics)
                {
                    CompileLog.Add(diagnostic.ToString());
                }

                if (!result.Success)
                {
                    CompileLog.Add("Compilation error");
                    return null;
                }

                CompileLog.Add("Compilation success!");

                stream.Seek(0, SeekOrigin.Begin);

                Assembly assemby = AppDomain.CurrentDomain.Load(stream.ToArray());
                return assemby;
            }
        }
    }
}