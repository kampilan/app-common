using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AppCommon.Persistence.Interceptors;

public class SqlLoggingInterceptor(ILogger logger): DbCommandInterceptor
{

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        
        logger.LogDebug(command.CommandText);
        
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        logger.LogDebug(command.CommandText);
        
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        
    }
    
}