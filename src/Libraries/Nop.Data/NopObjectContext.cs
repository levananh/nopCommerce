using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data.Mapping;

namespace Nop.Data
{
    /// <summary>
    /// Represents base object context
    /// </summary>
    public partial class NopObjectContext : DbContext, IDbContext
    {
        #region Ctor

        public NopObjectContext(DbContextOptions<NopObjectContext> options) : base(options)
        {
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// Further configuration the model
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //get methods to apply configurations
            var applyEntityTypeConfigurationMethod = typeof(NopObjectContext)
                .GetMethod(nameof(ApplyEntityTypeConfiguration), BindingFlags.Instance | BindingFlags.NonPublic);
            var applyQueryTypeConfigurationMethod = typeof(NopObjectContext)
                .GetMethod(nameof(ApplyQueryTypeConfiguration), BindingFlags.Instance | BindingFlags.NonPublic);

            //load all entity anf query type configurations
            var genericTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.BaseType?.IsGenericType ?? false);
            foreach (var genericType in genericTypes)
            {
                var applyConfigurationMethod = 
                    genericType.BaseType.GetGenericTypeDefinition() == typeof(NopEntityTypeConfiguration<>) ? applyEntityTypeConfigurationMethod 
                    : genericType.BaseType.GetGenericTypeDefinition() == typeof(NopQueryTypeConfiguration<>) ? applyQueryTypeConfigurationMethod 
                    : null;
                
                //dynamically invoke apply configuration method
                applyConfigurationMethod
                    ?.MakeGenericMethod(genericType.BaseType.GenericTypeArguments)
                    .Invoke(this, new object[] { modelBuilder, genericType });
            }

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Apply configuration that is defined by the passed type
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
        /// <param name="configurationType">Configuration type</param>
        protected virtual void ApplyEntityTypeConfiguration<TEntity>(ModelBuilder modelBuilder, Type configurationType) where TEntity : BaseEntity
        {
            modelBuilder.ApplyConfiguration((NopEntityTypeConfiguration<TEntity>)Activator.CreateInstance(configurationType));
        }

        /// <summary>
        /// Apply configuration that is defined by the passed type 
        /// </summary>
        /// <typeparam name="TQuery">Query type</typeparam>
        /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
        /// <param name="configurationType">Configuration type</param>
        protected virtual void ApplyQueryTypeConfiguration<TQuery>(ModelBuilder modelBuilder, Type configurationType) where TQuery : class
        {
            modelBuilder.ApplyConfiguration((NopQueryTypeConfiguration<TQuery>)Activator.CreateInstance(configurationType));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a DbSet that can be used to query and save instances of entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>A set for the given entity type</returns>
        public virtual new DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        /// <summary>
        /// Generate a script to create all tables for the current model
        /// </summary>
        /// <returns>A SQL script</returns>
        public virtual string GenerateCreateScript()
        {
            return this.Database.GenerateCreateScript();
        }

        /// <summary>
        /// Creates a LINQ query for the query type based on a raw SQL query
        /// </summary>
        /// <typeparam name="TQuery">Query type</typeparam>
        /// <param name="sql">The raw SQL query</param>
        /// <returns>An IQueryable representing the raw SQL query</returns>
        public virtual IQueryable<TQuery> QueryFromSql<TQuery>(string sql) where TQuery : class
        {
            return this.Query<TQuery>().FromSql(sql);
        }
        
        /// <summary>
        /// Creates a LINQ query for the entity based on a raw SQL query
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="sql">The raw SQL query</param>
        /// <param name="parameters">The values to be assigned to parameters</param>
        /// <returns>An IQueryable representing the raw SQL query</returns>
        public virtual IQueryable<TEntity> EntityFromSql<TEntity>(string sql, params object[] parameters) where TEntity : BaseEntity
        {
            //add parameters to sql
            for (var i = 0; i <= (parameters?.Length ?? 0) - 1; i++)
            {
                if (!(parameters[i] is DbParameter parameter))
                    continue;

                sql = $"{sql}{(i > 0 ? "," : string.Empty)} @{parameter.ParameterName}";

                //whether parameter is output
                if (parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Output)
                    sql = $"{sql} output";
            }
            
            return this.Set<TEntity>().FromSql(sql, parameters);
        }

        /// <summary>
        /// Executes the given SQL against the database
        /// </summary>
        /// <param name="sql">The SQL to execute</param>
        /// <param name="doNotEnsureTransaction">true - the transaction creation is not ensured; false - the transaction creation is ensured.</param>
        /// <param name="timeout">The timeout to use for command. Note that the command timeout is distinct from the connection timeout, which is commonly set on the database connection string</param>
        /// <param name="parameters">Parameters to use with the SQL</param>
        /// <returns>The number of rows affected</returns>
        public virtual int ExecuteSqlCommand(RawSqlString sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            //set specific command timeout
            var previousTimeout = this.Database.GetCommandTimeout();
            this.Database.SetCommandTimeout(timeout);

            var result = 0;
            if (!doNotEnsureTransaction)
            {
                //use with transaction
                using (var transaction = this.Database.BeginTransaction())
                {
                    result = this.Database.ExecuteSqlCommand(sql, parameters);
                    transaction.Commit();
                }
            }
            else
                result = this.Database.ExecuteSqlCommand(sql, parameters);
            
            //return previous timeout back
            this.Database.SetCommandTimeout(previousTimeout);
            
            return result;
        }

        /// <summary>
        /// Detach an entity from the context
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        public virtual void Detach<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entityEntry = this.Entry(entity);
            if (entityEntry == null)
                return;
            
            //set the entity is not being tracked by the context
            entityEntry.State = EntityState.Detached;
        }

        #endregion
    }
}