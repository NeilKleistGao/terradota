﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace terradota {
  internal class Generator {
    private readonly string mItemName;
    private readonly string mFilename;
    private CompilationUnitSyntax mRoot;
    private ClassDeclarationSyntax? mClass;
    private NamespaceDeclarationSyntax? mNamespace;
    private ClassDeclarationSyntax? mOriginCls;
    private NamespaceDeclarationSyntax? mOriginNS;
    private bool mUpdated = false;

    public Generator(string itemName, string outputFilename) {
      mItemName = itemName;
      mFilename = outputFilename;

      bool inited = true;
      if (!File.Exists(mFilename)) {
        var stream = File.Create(mFilename);
        stream.Close();
        inited = false;
      }

      var text = File.ReadAllText(mFilename);
      var syntaxTree = CSharpSyntaxTree.ParseText(text);
      mRoot = (CompilationUnitSyntax)syntaxTree.GetRoot();
      if (!inited) {
        Init();
        Generate();
      }

      mNamespace = mRoot.Members.Where((mem) => {
        var ns = mem as NamespaceDeclarationSyntax;
        if (ns != null) {
          return ns.Name.ToString().Equals("TerraDota.Items");
        }
        return false;
      }).First() as NamespaceDeclarationSyntax;
      if (mNamespace != null) {
        mOriginNS = mNamespace;
        mClass = mNamespace.Members.Where((mem) => {
          var cls = mem as ClassDeclarationSyntax;
          if (cls != null) {
            return cls.Identifier.Text.Equals(mItemName);
          }
          return false;
        }).First() as ClassDeclarationSyntax;

        if (mClass != null) {
          mOriginCls = mClass;
        }
      }
    }

    private UsingDirectiveSyntax Using(string ns) {
      return SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(ns));
    }

    private MethodDeclarationSyntax Func(string name, string type, bool isPublic, bool overrided, BlockSyntax body) {
      var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(type), name);
      method = method.WithBody(body);
      if (isPublic) {
        method = method.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
      }
      if (overrided) {
        method = method.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
      }

      return method;
    }

    private void Init() {
      mRoot = mRoot.AddUsings(Using("Terraria"));
      mRoot = mRoot.AddUsings(Using("Terraria.ID"));
      mRoot = mRoot.AddUsings(Using("Terraria.ModLoader"));

      var cls = SyntaxFactory.ClassDeclaration(mItemName);
      cls = cls.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
      cls = cls.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(PARENT)));
      cls = SetTooltip(cls, "");
      cls = cls.AddMembers(Func("SetDefaults", "void", true, true, SyntaxFactory.Block()));
      cls = cls.AddMembers(Func("AddRecipes", "void", true, true, SyntaxFactory.Block()));
      cls = cls.AddMembers(Func("UseItem", "bool?", true, true, SyntaxFactory.Block(
        SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))
        )));

      mClass = mOriginCls = cls;
      var ns = SyntaxFactory.NamespaceDeclaration(
        SyntaxFactory.IdentifierName(NAMESPACE),
        new SyntaxList<ExternAliasDirectiveSyntax>(),
        new SyntaxList<UsingDirectiveSyntax>(),
        new SyntaxList<MemberDeclarationSyntax>(cls)
      );
      mNamespace = mOriginNS = ns;

      mRoot = mRoot.AddMembers(ns);

      var comment = SyntaxFactory.Comment(HEAD);
      mRoot = mRoot.WithLeadingTrivia(comment);
    }

    private static readonly string HEAD = "/***automatically generated by terradota***/\n";
    private static readonly string NAMESPACE = "TerraDota.Items";
    private static readonly string PARENT = "ModItem";

    public void Generate() {
      if (mClass == null || mOriginCls == null || mOriginNS == null || mNamespace == null) {
        Console.Error.WriteLine($"can not generate item ${mItemName}.");
        return;
      }
      if (mUpdated) {
        mRoot = mRoot.Update(
          mRoot.Externs,
          mRoot.Usings,
          mRoot.AttributeLists,
          mRoot.Members.Replace(mOriginNS, mNamespace.ReplaceNode(mOriginCls, mClass)),
          mRoot.EndOfFileToken
        );
      }

      StreamWriter writer = new StreamWriter(mFilename);
      var result = Formatter.Format(mRoot, new AdhocWorkspace());
      writer.Write(result.ToFullString());
      writer.Close();
    }

    private bool isTargetMethod(MemberDeclarationSyntax member, string name) {
      if (member is MethodDeclarationSyntax method) {
        return method.Identifier.Text == name;
      }

      return false;
    }

    private ClassDeclarationSyntax UpdateClassMember(ClassDeclarationSyntax cls, string name, MemberDeclarationSyntax newMem) {
      var origins = (from mem in cls.Members
                     where isTargetMethod(mem, name)
                     select mem).ToList();
      if (origins.Count == 0) {
        mUpdated = true;
        return cls.AddMembers(newMem);
      }
      else {
        mUpdated = true;
        return cls.Update(
          cls.AttributeLists,
          cls.Modifiers,
          cls.Keyword,
          cls.Identifier,
          cls.TypeParameterList,
          cls.BaseList,
          cls.ConstraintClauses,
          cls.OpenBraceToken,
          cls.Members.Replace(origins.First(), newMem),
          cls.CloseBraceToken,
          cls.SemicolonToken
        );
      }
    }

    private ClassDeclarationSyntax SetTooltip(ClassDeclarationSyntax cls, string tip) {
      return UpdateClassMember(cls, "SetStaticDefaults", Func("SetStaticDefaults", "void", true, true, SyntaxFactory.Block(
        SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
          SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Tooltip"), SyntaxFactory.IdentifierName("SetDefault")
            ),
          SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            new ArgumentSyntax[] { SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(tip))) }
          ))
        )))));
    }

    public string Tooltip {
      set {
        if (mClass is ClassDeclarationSyntax cls) mClass = SetTooltip(cls, value);
      }
    }
  }
}
