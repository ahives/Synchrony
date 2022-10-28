namespace Synchrony.Persistence;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TransactionOperations")]
public class OperationEntity
{
    [Column("Id"), Key, Required]
    public Guid Id { get; init; }

    [Column("TransactionId"), Required]
    public Guid TransactionId { get; init; }

    [Column("Name"), Required]
    public string Name { get; init; }

    [Column("SequenceNumber")]
    public int SequenceNumber { get; init; }

    [Column("State")]
    public int State { get; set; }

    [Column("CreationTimestamp")]
    public DateTimeOffset CreationTimestamp { get; init; }
}