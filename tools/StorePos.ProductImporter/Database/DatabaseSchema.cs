namespace StorePos.ProductImporter.Database;

public sealed record SqlColumnDefinition(
    string TableName,
    string ColumnName,
    string TypeName,
    int MaxLength,
    byte Precision,
    byte Scale,
    bool IsNullable,
    bool IsIdentity);

public sealed record SqlIndexDefinition(
    string TableName,
    string IndexName,
    bool IsUnique,
    string? FilterDefinition,
    IReadOnlyList<string> Columns);

public sealed record SqlForeignKeyDefinition(
    string ParentTable,
    string ParentColumn,
    string ReferencedTable,
    string ReferencedColumn);

public sealed record DatabaseSchema(
    IReadOnlyList<SqlColumnDefinition> Columns,
    IReadOnlyList<SqlIndexDefinition> Indexes,
    IReadOnlyList<SqlForeignKeyDefinition> ForeignKeys,
    IReadOnlySet<string> CheckConstraints);

public sealed record SchemaValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsCompatible => Errors.Count == 0;
}
