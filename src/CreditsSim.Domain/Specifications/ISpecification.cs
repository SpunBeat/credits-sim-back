namespace CreditsSim.Domain.Specifications;

/// <summary>
/// Especificación composable sobre <see cref="IQueryable{T}"/> para traducir filtros a SQL vía EF Core.
/// </summary>
public interface ISpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}
