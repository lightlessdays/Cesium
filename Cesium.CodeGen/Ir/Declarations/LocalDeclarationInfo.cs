using Cesium.Ast;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Declarations;

/// <summary>
/// A local declaration info, as opposed to <see cref="IScopedDeclarationInfo"/>, cannot be a typedef or have an
/// initializer, and is always a part of a more complex syntax construct: say, a parameter declaration or a function
/// definition.
/// </summary>
internal record LocalDeclarationInfo(
    IType Type,
    string? Identifier,
    ParametersInfo? Parameters,
    string? CliImportMemberName)
{
    public static LocalDeclarationInfo Of(IList<IDeclarationSpecifier> specifiers, Declarator? declarator)
    {
        var (type, cliImportMemberName) = ProcessSpecifiers(specifiers);
        if (declarator == null)
            return new LocalDeclarationInfo(type, null, null, null);

        var (pointer, directDeclarator) = declarator;
        if (pointer != null)
        {
            var (typeQualifiers, childPointer) = pointer;
            if (typeQualifiers != null || childPointer != null)
                throw new NotImplementedException($"Complex pointer type is not supported, yet: {pointer}.");

            type = new PointerType(type);
        }

        string? identifier = null;
        ParametersInfo? parameters = null;

        var currentDirectDeclarator = directDeclarator;
        while (currentDirectDeclarator != null)
        {
            switch (currentDirectDeclarator)
            {
                case IdentifierListDirectDeclarator list:
                {
                    var (_, identifiers) = list;
                    if (identifiers != null)
                        throw new NotImplementedException(
                            "Non-empty identifier list inside of a direct declarator is not supported, yet:" +
                            $" {string.Join(", ", identifiers)}");
                    break;
                }

                case IdentifierDirectDeclarator identifierD:
                    if (identifier != null)
                        throw new NotSupportedException(
                            $"Second identifier \"{identifierD.Identifier}\" given for the declaration \"{identifier}\".");
                    identifier = identifierD.Identifier;
                    break;

                case ParameterListDirectDeclarator parametersD:
                    if (parameters != null)
                        throw new NotSupportedException(
                            $"Second parameters list declarator for an entity already having one: {parametersD}.");

                    parameters = ParametersInfo.Of(parametersD.Parameters);
                    break;

                case ArrayDirectDeclarator array:
                    var (_, typeQualifiers, size) = array;
                    if (typeQualifiers != null)
                        throw new NotImplementedException(
                            $"Array type qualifiers aren't supported, yet: {string.Join(", ", typeQualifiers)}");
                    if (size != null)
                        throw new NotImplementedException(
                            $"Array with specified size isn't supported, yet: {array}.");

                    type = new PointerType(type);
                    break;

                default: throw new NotImplementedException($"Direct declarator not supported, yet: {currentDirectDeclarator}.");
            }

            currentDirectDeclarator = currentDirectDeclarator.Base;
        }

        return new LocalDeclarationInfo(type, identifier, parameters, cliImportMemberName);
    }

    private static (IType, string? cliImportMemberName) ProcessSpecifiers(
        IList<IDeclarationSpecifier> specifiers)
    {
        IType? type = null;
        var isConst = false;
        string? cliImportMemberName = null;
        foreach (var specifier in specifiers)
        {
            switch (specifier)
            {
                case SimpleTypeSpecifier ts:
                    if (type != null)
                        throw new NotSupportedException(
                            $"Unsupported type definition after already resolved type {type}: {ts}.");

                    type = new PrimitiveType(ts.TypeName switch
                    {
                        "char" => PrimitiveTypeKind.Char,
                        "int" => PrimitiveTypeKind.Int,
                        "void" => PrimitiveTypeKind.Void,
                        var unknown =>
                            throw new NotImplementedException($"Not supported yet type specifier: {unknown}.")
                    });
                    break;

                case TypeQualifier tq:
                    switch (tq.Name)
                    {
                        case "const":
                            if (isConst)
                                throw new NotSupportedException(
                                    $"Multiple const specifiers: {string.Join(", ", specifiers)}.");
                            isConst = true;
                            break;
                        default:
                            throw new NotSupportedException($"Type qualifier {tq} is not supported, yet.");
                    }

                    break;

                case CliImportSpecifier cis:
                    if (cliImportMemberName != null)
                        throw new NotSupportedException(
                            $"Multiple CLI import specifiers on a declaration among {string.Join(", ", specifiers)}.");

                    cliImportMemberName = cis.MemberName;
                    break;

                case StorageClassSpecifier { Name: "typedef" }:
                    throw new NotSupportedException($"typedef not expected: {string.Join(", ", specifiers)}.");

                case StructOrUnionSpecifier typeSpecifier:
                {
                    if (type != null)
                        throw new NotSupportedException(
                            $"Cannot update type {type} with a struct specifier {typeSpecifier}.");

                    var (complexTypeKind, identifier, structDeclarations) = typeSpecifier;
                    if (complexTypeKind != ComplexTypeKind.Struct)
                        throw new NotImplementedException($"Complex type kind not supported, yet: {complexTypeKind}.");

                    if (identifier != null)
                        throw new NotImplementedException($"Named structures aren't supported, yet: {identifier}.");

                    type = new StructType(structDeclarations);
                    break;
                }

                default:
                    throw new NotImplementedException($"Declaration specifier {specifier} isn't supported, yet.");
            }
        }

        if (type == null)
            throw new NotSupportedException(
                $"Declaration specifiers missing type specifier: {string.Join(", ", specifiers)}");

        return (isConst ? new ConstType(type) : type, cliImportMemberName);
    }
}