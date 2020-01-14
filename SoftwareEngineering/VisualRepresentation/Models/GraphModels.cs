﻿using DependenceFinderAndPlotter;
using DependenceFinderAndPlotter.Finders;
using DependenceFinderAndPlotter.Nodes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace VisualRepresentation.Models
{
    class GraphModels
    {
        public Graph GenerateGraph(bool story1, bool story2, bool story3, List<string> VMFoundFiles)
        {
            //var story1Graph = generateStory1Graph(VMFoundFiles);
            var story2Graph = generateStory2Graph(VMFoundFiles, true);
            return story2Graph;
        }
      
        private Graph generateStory2Graph(List<string> VMFoundFiles, bool ignoreUnknownDeclaration)
        {
            Graph resultGraph = new Graph("MethodsGraph");
            var declarationFinder = new MethodDeclarationsFinder();
            var declarationsInFiles = new List<MethodDeclarationSyntax>();
            
            foreach (var path in VMFoundFiles)
            {
                var foundDeclarations = declarationFinder.GetMethodsDecalarationsInFIle(path);
                declarationsInFiles.AddRange(foundDeclarations);
            }

            var knownDeclarationsNames = declarationsInFiles.Select(e => e.Identifier.Text).ToList();

            List<MethodInvocationsInDeclaration> InvocationsInDeclarations = new List<MethodInvocationsInDeclaration>();
            var invocationFinder = new MethodInvocationsFinder();
            
            foreach (var declaration in declarationsInFiles)
            {
                List<string> resultInvocations = new List<string>();
                var invocationsInDeclaration = invocationFinder.GetMethodsInvocationsDeclaration(declaration);
                if (ignoreUnknownDeclaration)
                {
                    foreach (var invocation in invocationsInDeclaration)
                    {
                        var expr = invocation.Expression;
                        if (expr is IdentifierNameSyntax)
                        {
                            IdentifierNameSyntax identifierName = expr as IdentifierNameSyntax;
                            // identifierName is your method name
                            if (knownDeclarationsNames.Contains(identifierName.Identifier.Text))
                            {
                                resultInvocations.Add(identifierName.Identifier.Text);
                            }

                        }

                        if (expr is MemberAccessExpressionSyntax)
                        {
                            MemberAccessExpressionSyntax memberAccessExpressionSyntax = expr as MemberAccessExpressionSyntax;
                            //memberAccessExpressionSyntax.Name is your method name
                            if (knownDeclarationsNames.Contains(memberAccessExpressionSyntax.Name.Identifier.Text))
                            {
                                resultInvocations.Add(/*invocation.Expression.*/"MemberAccessExpressionSyntax");
                            }
                        }
                    }

                    MethodInvocationsInDeclaration semiResult = new MethodInvocationsInDeclaration()
                    {
                        MethodInvocations = invocationsInDeclaration,
                        InDeclaration = declaration
                    };

                    InvocationsInDeclarations.Add(semiResult);
                }

            }

            foreach (MethodInvocationsInDeclaration invocationsInDeclaration in InvocationsInDeclarations)
            {
                var declarationNode = resultGraph.AddNode(invocationsInDeclaration.InDeclaration.Identifier.ToString());
                declarationNode.Attr.FillColor = Color.AliceBlue;
                declarationNode.Attr.Shape = Shape.Diamond;
                foreach (var invocation in invocationsInDeclaration.MethodInvocations)
                {
                    var invocationName = getInvocationName(invocation);
                    var invocationNode = resultGraph.FindNode(invocationName);
                    if (invocationNode is null)
                    {
                        resultGraph.AddNode(invocationName);
                    }
                    var invocationInDeclarationEdge = resultGraph.AddEdge(declarationNode.LabelText+"(){}", invocationName);
                }
            }

            return resultGraph;

        }

        #region Usings graph
        private Graph generateStory1Graph(List<string> VMFoundFiles)
        {
            //TODO: add other cases than usings: partial classes, interfaces, inheritance
            Graph resultGraph = new Graph("UsingsGraph");
            var usingsInFilesResult = new List<UsingDirectivesInFile>();
            var usingDirectiveFinder = new UsingDirectiveFinder();
            foreach (var path in VMFoundFiles)
            {
                var singleFileUsings = new UsingDirectivesInFile()
                {
                    InFile = this.splitPath(path),
                    UsingDirectives = usingDirectiveFinder.GetUsingDirecitvesInFileRoslyn(path),
                    fileSize = getFileSize(path)
                };
                usingsInFilesResult.Add(singleFileUsings);
            }

            foreach (var usingNode in usingsInFilesResult)
            {
                string fileNodeName = usingNode.InFile + "\n" + usingNode.fileSize;
                var fileNode = resultGraph.AddNode(fileNodeName);
                fileNode.Attr.FillColor = Color.AliceBlue;
                fileNode.Attr.Shape = Shape.Diamond;
                //var fileNode = resultGraph.FindNode(usingNode.InFile);


                foreach (var usingInFile in usingNode.UsingDirectives)
                {
                    string usingInFileNodeName = usingInFile.Name.ToString();
                    var node = resultGraph.FindNode(usingInFileNodeName);
                    if (node is null)
                    {
                        resultGraph.AddNode(usingInFileNodeName);
                        
                    }
                    var createdNode = resultGraph.FindNode(usingInFileNodeName);
                    var usingInFileEdge = resultGraph.AddEdge(fileNodeName, usingInFileNodeName);
                    

                    var thatUsingCount = usingNode.UsingDirectives.Where(e => e.Name == usingInFile.Name).Count();
                    usingInFileEdge.LabelText = thatUsingCount.ToString();
                }
            }
            resultGraph.LayoutAlgorithmSettings.NodeSeparation = 10;
            resultGraph.Attr.OptimizeLabelPositions = true;
            return resultGraph;
        }
        #endregion

        private string getInvocationName(InvocationExpressionSyntax _invocation)
        {
            var expression = _invocation.Expression;
            string result = expression.ToString() + "()";

            return result;
        }

        private string getFileSize(string path)
        {
            var lines = 0;
            var characters = File.ReadAllLines(path).Sum(s => s.Length);
            using (var reader = File.OpenText(path))
            {
                while (reader.ReadLine() != null)
                {
                    lines++;
                }
            }

            string result = $"{lines.ToString()}\n({characters.ToString()})";
            return result;
        }
        

        private string splitPath(string pathToSplit)
        {
            string result = string.Empty;
            char[] pathSeparator = { '\\' };
            var splits = pathToSplit.Split(pathSeparator);
            if (splits.Last() == "Program.cs")
            {
                result = splits[splits.Length - 2] + "\\" + splits[splits.Length - 1];
                return result;
            }
            else
            {
                return splits.Last();
            }
        }
    }
}