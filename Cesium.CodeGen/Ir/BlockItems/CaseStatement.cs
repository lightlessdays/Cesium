using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CaseStatement : IBlockItem
{
    public CaseStatement(Ast.CaseStatement statement)
    {
        var (constant, body) = statement;

        Expression = constant?.ToIntermediate();
        Statement = body.ToIntermediate();
    }

    public string Label { get; } = Guid.NewGuid().ToString();
    public IBlockItem Statement { get; }
    public IExpression? Expression { get; }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // TODO[#408]: optimize multiple cases at once

        if (scope is not BlockScope sws || sws.SwitchCases == null)
            throw new AssertException("Cannot use case statement outside of switch");

        sws.SwitchCases.Add(new SwitchCase(Expression, Label));

        return new LabelStatement(Label, Statement).Lower(scope);
    }

    public void EmitTo(IEmitScope scope)
    {
        throw new AssertException("Cannot emit case statement independently.");
    }
}
