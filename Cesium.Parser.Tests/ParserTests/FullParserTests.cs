using Cesium.Test.Framework;
using Xunit.Sdk;
using Yoakke.C.Syntax;
using Yoakke.Streams;

namespace Cesium.Parser.Tests.ParserTests;

public class FullParserTests : ParserTestBase
{
    private static Task DoTest(string source)
    {
        var lexer = new CLexer(source);
        var parser = new CParser(lexer);

        var result = parser.ParseTranslationUnit();
        Assert.True(result.IsOk, GetErrorString(result));

        var token = parser.TokenStream.Peek();
        if (token.Kind != CTokenType.End)
            throw new XunitException($"Excessive output after the end of a translation unit at {lexer.Position}: {token.Kind} {token.Text}.");

        var serialized = JsonSerialize(result.Ok.Value);
        return Verify(serialized);
    }

    [Fact]
    public Task MinimalProgramTest() => DoTest("int main() {}");

    [Fact]
    public Task ReturnTest() => DoTest("int main() { return 0; }");

    [Fact]
    public Task ExpressionTest() => DoTest("int main() { return 2 + 2 * 2; }");

    [Fact]
    public Task SimpleVariableTest() => DoTest("void foo() { int x = 0; }");

    [Fact]
    public Task VariableTest() => DoTest("int main() { int x = 0; return x + 1; }");

    [Fact]
    public Task VariableArithmeticTest() => DoTest(@"int main()
{
    int x = 0;
    x = x + 1;
    return x + 1;
 }");

    [Fact]
    public Task FunctionCallTest() => DoTest(@"int foo()
{
    return 42;
}

int main()
{
    int f = foo();
    return foo() + 1;
}");

    [Fact]
    public Task CliImport() => DoTest(@"__cli_import(""Foo.Bar::Baz"")
int foo();

int main()
{
    int f = foo();
    return foo() + 1;
}");

    [Fact]
    public Task StringLiteral() => DoTest(@"int main()
{
    test(""hello world"");
}");

    [Fact]
    public Task CharLiteral() => DoTest(@"int main()
{
    char x = 'c';
    char y = '\t';
}");

    [Fact]
    public Task StringVariable() => DoTest(@"int main()
{
    const char *foo = ""hello world"";
}");

    [Fact]
    public Task NegationTest() => DoTest("void foo() { int x = -42; }");

    [Fact]
    public Task IntParameter() => DoTest("void foo(int x){}");

    [Fact]
    public Task PointerParameter() => DoTest("void foo(char *x){}");

    [Fact]
    public Task ArrayParameter() => DoTest("void foo(char x[]){}");

    [Fact]
    public Task ArrayOfArrayParameter() => DoTest("void foo(char x[][]){}");

    [Fact]
    public Task AnonymousArrayParameter() => DoTest("void foo(char[]){}");

    [Fact]
    public Task PointerArrayParameter() => DoTest("void foo(char *x[]){}");

    [Fact]
    public Task MainSignature() => DoTest("int main(int argc, char *argv[]){}");
}
