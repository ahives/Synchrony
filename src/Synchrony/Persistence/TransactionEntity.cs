namespace Synchrony.Persistence;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Transactions")]
public class TransactionEntity
{
    [Column("Id"), Key, Required]
    public Guid Id { get; init; }
    
    [Column("State")]
    public int State { get; set; }
    
    [Column("CreationTimestamp")]
    public DateTimeOffset CreationTimestamp { get; init; }
}