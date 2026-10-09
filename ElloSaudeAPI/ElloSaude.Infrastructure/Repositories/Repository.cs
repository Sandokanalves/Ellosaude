using Microsoft.EntityFrameworkCore;
using ElloSaude.Application.Common.Interfaces;
using System.Linq.Expressions;

namespace ElloSaude.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    // Criamos um DbSet fixo para evitar chamadas repetidas ao .Set<T>()
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
    }
    public IQueryable<T> AsQueryable()
    {
        // AsNoTracking é essencial para performance em grandes volumes
        return _dbSet.AsNoTracking();
    }

    // Adicionamos .AsNoTracking() para que consultas de busca sejam muito mais rápidas
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(ct);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // FindAsync não aceita AsNoTracking, mas é otimizado para buscar pela PK
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }

    public void Update(T entity)
    {
        // O Update do EF marca todas as propriedades como modificadas. 
        // É seguro e robusto para o seu Unit of Work.
        _dbSet.Update(entity);
    }
}