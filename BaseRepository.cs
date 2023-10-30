using IMatch.Common.Extensions;
using IMatch.Common.Interface;
using IMatch.DataAccess.Entities;
using IMatch.DataAccess.Persistence;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace IMatch.DataAccess.Base
{
    /// <summary>
    /// BaseRepository
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : EntityBase, new()
    {
        private readonly IMatchContext _context;
        private readonly IMemoryCache _cache;
        private MemoryCacheEntryOptions cacheOptions;       

        public BaseRepository(IMatchContext db, IMemoryCache cache)
        {
            _context = db;
            _cache = cache;
            cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1));
        }
      

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<T> Add(T entity)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Set<T>().Add(entity);
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                return entity;
            }
        }

        /// <summary>
        /// Add multiple
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<List<T>> AddRange(List<T> entities)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Set<T>().AddRange(entities);
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                return entities;
            }
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task Delete(T entity)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Entry(entity).State = EntityState.Deleted;
                    _context.Set<T>().Remove(entity);
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// FindWithSpecification
        /// </summary>
        /// <param name="specification"></param>
        /// <returns></returns>
        public IList<T> FindWithSpecification(ISpecification<T> specification = null)
        {
            var key = GetCacheKey(specification);
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(cacheOptions);
                var query = SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), specification);
                return query.AsNoTracking().ToList();
            });
        }

        /// <summary>
        /// FindWithSpecification
        /// </summary>
        /// <param name="specification"></param>
        /// <returns></returns>
        public IList<T> FindWithSpecificationWithTracking(ISpecification<T> specification = null)
        {
            return SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), specification).ToList();
        }
        /// <summary>
        /// GetAll
        /// </summary>
        /// <returns></returns>
        public async Task<IList<T>> GetAll()
        {
            var key = GetCacheKey();
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.SetOptions(cacheOptions);
                return await _context.Set<T>().ToListAsync();
            });
        }

        /// <summary>
        /// GetAllByCriteria
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> GetAllByCriteria()
        {
            var key = GetCacheKey();
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(cacheOptions);
                return _context.Set<T>().AsNoTracking().AsQueryable();
            });
        }

        // <summary>
        /// GetAllByCriteria
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> GetAllByCriteriaWithTracking()
        {
            var key = GetCacheKey();
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(cacheOptions);
                return _context.Set<T>().AsQueryable();
            });
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <param name="queryStatement"></param>
        /// <returns></returns>
        public IQueryable<T> Query(string queryStatement)
        {
            var key = GetCacheKey();
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(cacheOptions);
                return _context.Set<T>().SqlQuery(queryStatement).AsNoTracking().AsQueryable();
            });
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <param name="queryStatement"></param>
        /// <returns></returns>
        public IQueryable<T> Query(string queryStatement, params object[] parameters)
        {
            var key = GetCacheKey();
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(cacheOptions);
                return _context.Set<T>().SqlQuery(queryStatement, parameters).AsNoTracking().AsQueryable();
            });
        }

        /// <summary>
        /// SQLQuery
        /// </summary>
        /// <typeparam name="DTO"></typeparam>
        /// <param name="queryStatement"></param>
        /// <returns></returns>
        public IQueryable<DTO> SQLQuery<DTO>(string queryStatement)
        {
            var key = queryStatement;
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(cacheOptions);
                return _context.Database.SqlQuery<DTO>(queryStatement).AsQueryable();
            });
        }

        /// <summary>
        /// SQLQuery
        /// </summary>
        /// <typeparam name="DTO"></typeparam>
        /// <param name="queryStatement"></param>
        /// <returns></returns>
        public IQueryable<DTO> SQLQuery<DTO>(string queryStatement, params object[] parameters)
        {
            var key = GetCacheKey(typeof(DTO));

            // Split the input text into an array of substrings
            var parameterList = ExtractParameters(queryStatement);

            List<SqlParameter> sqlParameters = new List<SqlParameter>();

            for (int index = 0; index < parameterList.Count; index++)
            {
                sqlParameters.Add(new SqlParameter("@" + parameterList[index], parameters[index]));
            }
            try
            {
                var result = _context.Database.SqlQuery<DTO>(queryStatement, sqlParameters.ToArray()).AsQueryable();

                return result;
            }
            finally
            {
                // Clear the parameters after the query execution
                //sqlParameters.ForEach(p => p.Value = null);
            }
        }

        /// <summary>
        /// GetById
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<T> GetById(int id, bool track = false)
        {
            return track ? await _context.Set<T>().FirstOrDefaultAsync(x=>x.ID == id) : await _context.Set<T>().AsNoTracking().FirstOrDefaultAsync(x=>x.ID == id);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<T> Update(T entity)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Entry(entity).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return entity;
        }

        /// <summary>
        /// ExecuteCommand
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<int> ExecuteCommand(string commandName, List<SqlParameter> parameters)
        {
            try
            {
                var command = _context.Database.Connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = commandName;
                command.Parameters.AddRange(parameters.ToArray());

                if (command.Connection.State != ConnectionState.Open)
                    await command.Connection.OpenAsync();

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (command.Connection.State != ConnectionState.Closed)
                    command.Connection.Close();

                return rowsAffected;
            }
            catch
            {
                throw;
            }
        }
        public async Task<object> ExecuteReaderCommand(string commandName, List<SqlParameter> parameters)
        {
            try
            {
                var command = _context.Database.Connection.CreateCommand();
                if (command.Connection.State != ConnectionState.Open)
                    await command.Connection.OpenAsync();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = commandName;

                // Build the SQL command with parameters
                command.Parameters.AddRange(parameters.ToArray());

                // Execute the stored procedure with parameters
                var reader = await command.ExecuteReaderAsync();
                var resultSets = new List<object>();

                do
                {
                    List<object> rows = new List<object>();

                    while (reader.Read())
                    {
                        var cells = new Object[reader.FieldCount];
                        int fieldCount = reader.GetValues(cells);

                        rows.Add(cells);
                    }

                    if (rows.Count > 0)
                    {
                        resultSets.Add(rows);
                    }
                }
                while (reader.NextResult());

                 if (command.Connection.State != ConnectionState.Closed)
                    command.Connection.Close();

                return resultSets;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private string GetCacheKey(ISpecification<T> specification = null)
        {
            var key = typeof(T).Name + System.Reflection.MethodBase.GetCurrentMethod();
            if (specification != null)
            {
                key = typeof(T).Name + specification.ToString() + Guid.NewGuid().ToString() + System.Reflection.MethodBase.GetCurrentMethod();
            }
            return key;
        }

        private string GetCacheKey(Type type)
        {
            return type.Name + System.Reflection.MethodBase.GetCurrentMethod();
        }

        private List<string> ExtractParameters(string query)
        {
            // Define a regular expression pattern to match @parameters
            string pattern = @"@(\w+)";

            // Create a Regex object with the pattern
            Regex regex = new Regex(pattern);

            // Find all matches in the query
            MatchCollection matches = regex.Matches(query);

            // Create a list to store the parameter names
            List<string> parameterNames = new List<string>();

            // Extract parameter names from matches
            foreach (Match match in matches)
            {
                parameterNames.Add(match.Groups[1].Value);
            }

            return parameterNames;
        }
    }

}
