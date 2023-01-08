﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NonAwaitedAsync
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NonAwaitedAsyncAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NonAwaitedAsync";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information

            context.RegisterSyntaxNodeAction(AnalyzeSymbolNode, SyntaxKind.InvocationExpression);
        }
        private void AnalyzeSymbolNode(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is InvocationExpressionSyntax node)
            {
                if (syntaxNodeAnalysisContext
                        .SemanticModel
                        .GetSymbolInfo(node.Expression, syntaxNodeAnalysisContext.CancellationToken)
                        .Symbol is IMethodSymbol methodSymbol)
                {
                    //AwaitExpression
                    if (node.Parent is AwaitExpressionSyntax)
                    {
                        return;
                    }

                    var taskNamedTypeSymbol = syntaxNodeAnalysisContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Task).FullName);
                    if (methodSymbol.ReturnType.Equals(taskNamedTypeSymbol))
                    {
                        // For all such symbols, produce a diagnostic.
                        var diagnostic =
                            Diagnostic.Create(Rule, node.GetLocation(), methodSymbol.ToDisplayString());

                        syntaxNodeAnalysisContext.ReportDiagnostic(diagnostic);
                    }

                    if (methodSymbol.ReturnType is INamedTypeSymbol symbol)
                    {
                        if (symbol.IsGenericType && symbol.BaseType.Equals(taskNamedTypeSymbol))
                        {
                            // For all such symbols, produce a diagnostic.
                            var diagnostic =
                                Diagnostic.Create(Rule, node.GetLocation(), methodSymbol.ToDisplayString());

                            syntaxNodeAnalysisContext.ReportDiagnostic(diagnostic);
                        }
                    }

                }
            }
        }

    }
}