using System.Data;
using System.Data.Common;
using SnapApp.Svc.DbModels;
using SnapApp.Svc.Models;

namespace SnapApp.Svc.Services;

public interface IDatabaseService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task<LoginInfo?> GetLoginInfoByUserIdAsync(Guid userId);
    Task<UserInfo?> GetUserInfoByIdAsync(Guid id);
    Task DeleteLoginByUserIdAsync(Guid userId);
    Task<int?> UpsertLoginAsync(Login login);
}

internal static class DbCommandExtensions
{
    public static DbCommand AddParameter(this DbCommand cmd, string name, DbType type, object value)
    {
        DbParameter param = cmd.CreateParameter();
        param.ParameterName = name;
        param.DbType = type;
        param.Value = value;
        cmd.Parameters.Add(param);

        return cmd;
    }

    public static DbCommand AddParameter(this DbCommand cmd, string name, DbType type, out DbParameter outParam)
    {
        outParam = cmd.CreateParameter();
        outParam.ParameterName = name;
        outParam.DbType = type;
        outParam.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(outParam);

        return cmd;
    }
}

internal static class DbDataReaderExtensions
{
    public static List<T> Parse<T>(this DbDataReader reader)
    {
        List<T> objs = [];
        if (reader.HasRows)
        {
            Type objType = typeof(T);
            IEnumerable<string> objProps = objType.GetProperties().Select(f => f.Name);
            List<string> columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            while(reader.Read())
            {
                object? obj = Activator.CreateInstance(objType);

                if (obj != null)
                {
                    bool isObjEmpty = true;

                    foreach(string prop in objProps)
                    {
                        if (columns.Contains(prop, StringComparer.CurrentCultureIgnoreCase))
                        {
                            string col = columns.Single(c => c.Equals(prop, StringComparison.CurrentCultureIgnoreCase));
                            Type colType = reader.GetFieldType(col);
                            Type propType = objType.GetProperty(prop)!.PropertyType;
                            object? colValue = reader.GetValue(col);

                            if (colValue == DBNull.Value)
                            {
                                colValue = colType.IsValueType ? Activator.CreateInstance(colType) : null;
                            }
                            else if (propType.IsEnum)
                            {
                                colValue = Enum.Parse(propType, colValue.ToString()!);
                            }

                            objType.GetProperty(prop)!.SetValue(obj, colValue);
                            isObjEmpty = false;
                        }
                    }

                    if (!isObjEmpty)
                    {
                        objs.Add((T)obj);
                    }
                }
            }
        }

        return objs;
    }
}

public class DatabaseContext(DbConnection ctn) : IDatabaseService
{
    public async Task ConnectAsync()
    {
        if (ctn.State != ConnectionState.Open)
        {
            await ctn.OpenAsync();
        }
    }

    public async Task DisconnectAsync()
    {
        if (ctn.State == ConnectionState.Open)
        {
            await ctn.CloseAsync();
        }
    }

    public async Task<LoginInfo?> GetLoginInfoByUserIdAsync(Guid userId)
    {
        using DbCommand cmd = ctn.CreateCommand().AddParameter("@userId", DbType.Guid, userId);

        cmd.CommandText = "GetLoginInfoByUserId";
        cmd.CommandType = CommandType.StoredProcedure;
        await ConnectAsync();
        using DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        List<LoginInfo> logins = reader.Parse<LoginInfo>();

        return logins.Count != 0 ? logins.First() : null;
    }

    public async Task<UserInfo?> GetUserInfoByIdAsync(Guid id)
    {
        using DbCommand cmd = ctn.CreateCommand().AddParameter("@id", DbType.Guid, id);

        cmd.CommandText = "GetUserInfoById";
        cmd.CommandType = CommandType.StoredProcedure;
        await ConnectAsync();
        using DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        List<UserInfo> users = reader.Parse<UserInfo>();

        return users.Count != 0 ? users.First() : null;
    }

    public async Task DeleteLoginByUserIdAsync(Guid userId)
    {
        using DbCommand cmd = ctn.CreateCommand().AddParameter("@userId", DbType.Guid, userId);

        cmd.CommandText = "DeleteLoginByUserId";
        cmd.CommandType = CommandType.StoredProcedure;
        await ConnectAsync();

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    public async Task<int?> UpsertLoginAsync(Login login)
    {
        using DbCommand cmd = ctn.CreateCommand();

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.AddParameter("@userId", DbType.Guid, login.UserId);
        cmd.AddParameter("@cryptoKeys", DbType.Binary, login.CryptoKeys);
        cmd.AddParameter("@refreshTokenId", DbType.Guid, login.RefreshTokenId);
        cmd.AddParameter("@expiresOn", DbType.DateTime2, login.ExpiresOn);
        cmd.AddParameter("@createdOn", DbType.DateTime2, login.CreatedOn);

        await ConnectAsync();

        try
        {
            if (login.Id == null)
            {
                cmd.AddParameter("@id", DbType.Int32, out var outParam);
                cmd.CommandText = "InsertLogin";
                await cmd.ExecuteNonQueryAsync();

                return (int?)outParam.Value;
            }
            else
            {
                cmd.CommandText = "UpdateLogin";
                cmd.AddParameter("@id", DbType.Int32, login.Id);
                await cmd.ExecuteNonQueryAsync();

                return login.Id;
            }
        }
        finally
        {
            await DisconnectAsync();
        }
    }
}