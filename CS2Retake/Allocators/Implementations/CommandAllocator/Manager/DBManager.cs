using CS2Retake.Allocators.Implementations.CommandAllocator.Interfaces;
using CS2Retake.Allocators.Implementations.CommandAllocator.Repository;
using CS2Retake.Allocators.Implementations.CommandAllocator.Utils;
using CS2Retake.Configs;
using CS2Retake.Utils;
using CSZoneNet.Plugin.CS2BaseAllocator.Interfaces;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2Retake.Allocators.Implementations.CommandAllocator.Manager
{
    public class DBManager : IRetakeRepository
    {
        private static DBManager? _instance = null;

        public DBType DBType { get; set; } = DBType.Cache;
        public string AllocatorConfigDirectoryPath { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;

        private IRetakeRepository? _retakeDB = null;

        public static DBManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DBManager();
                }
                return _instance;
            }
        }

        public DBManager() { }

        public void Init()
        {
            var requestedDbType = this.DBType;
            var effectiveDbType = this.ResolveEffectiveDbType(requestedDbType);
            var effectiveSqlitePath = this.ResolveSQLiteStorageDirectory();

            this.DBType = effectiveDbType;

            MessageUtils.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                $"Init DBManager - Requested: {requestedDbType}, Effective: {effectiveDbType}, Path: {effectiveSqlitePath}");

            try
            {
                switch (effectiveDbType)
                {
                    case DBType.Cache:
                        this._retakeDB = null;
                        break;
                    case DBType.SQLite:
                        this._retakeDB = new SQLiteRepository(effectiveSqlitePath);
                        break;
                    case DBType.PostgreSql:
                        this._retakeDB = new PostgreSqlRepository(this.ConnectionString);
                        break;
                    default:
                        this._retakeDB = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageUtils.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"DB initialization failed for provider {effectiveDbType}. Falling back to Cache mode. Details: {ex.Message}");

                this.DBType = DBType.Cache;
                this._retakeDB = null;
            }
        }

        private string ResolveSQLiteStorageDirectory()
        {
            if (!string.IsNullOrWhiteSpace(RuntimeConfig.ModuleDirectory))
            {
                return Path.Join(RuntimeConfig.ModuleDirectory, "data", "CommandAllocator");
            }

            if (!string.IsNullOrWhiteSpace(this.AllocatorConfigDirectoryPath))
            {
                return Path.Join(this.AllocatorConfigDirectoryPath, "data");
            }

            return Path.Join(AppContext.BaseDirectory, "CS2RetakeData", "CommandAllocator");
        }

        private DBType ResolveEffectiveDbType(DBType requestedDbType)
        {
            if (requestedDbType != DBType.PostgreSql)
            {
                return requestedDbType;
            }

            if (this.TryValidatePostgreSqlConnectionString(out var validationReason))
            {
                return DBType.PostgreSql;
            }

            MessageUtils.Log(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                $"PostgreSQL was selected, but the connection string is not configured correctly ({validationReason}). Falling back to Cache mode.");

            return DBType.Cache;
        }

        private bool TryValidatePostgreSqlConnectionString(out string reason)
        {
            if (string.IsNullOrWhiteSpace(this.ConnectionString))
            {
                reason = "empty connection string";
                return false;
            }

            var conn = this.ConnectionString.Trim();

            // Ignore the default template/placeholder value from generated configs.
            if (conn.Contains("<server>", StringComparison.OrdinalIgnoreCase)
                || conn.Contains("<dbName>", StringComparison.OrdinalIgnoreCase)
                || conn.Contains("<username>", StringComparison.OrdinalIgnoreCase)
                || conn.Contains("<password>", StringComparison.OrdinalIgnoreCase))
            {
                reason = "template placeholders detected";
                return false;
            }

            try
            {
                var builder = new NpgsqlConnectionStringBuilder(conn);

                if (string.IsNullOrWhiteSpace(builder.Host))
                {
                    reason = "missing host/server";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(builder.Database))
                {
                    reason = "missing database";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(builder.Username))
                {
                    reason = "missing user id/username";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(builder.Password))
                {
                    reason = "missing password";
                    return false;
                }

                reason = "valid";
                return true;
            }
            catch (Exception ex)
            {
                reason = $"invalid format: {ex.Message}";
                return false;
            }
        }

        public bool InsertOrUpdateFullBuyPrimaryWeaponString(ulong userId, string weaponString, int team)
        {

            if(this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return false;
            }

            return this._retakeDB.InsertOrUpdateFullBuyPrimaryWeaponString(userId, weaponString, team);
        }

        public bool InsertOrUpdateFullBuySecondaryWeaponString(ulong userId, string weaponString, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return false;
            }

            return this._retakeDB.InsertOrUpdateFullBuySecondaryWeaponString(userId, weaponString, team);
        }

        public bool InsertOrUpdateFullBuyAWPChance(ulong userId, int chance, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return false;
            }

            return this._retakeDB.InsertOrUpdateFullBuyAWPChance(userId, chance, team);
        }

        public bool InsertOrUpdateMidPrimaryWeaponString(ulong userId, string weaponString, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return false;
            }

            return this._retakeDB.InsertOrUpdateMidPrimaryWeaponString(userId, weaponString, team);
        }

        public bool InsertOrUpdateMidSecondaryWeaponString(ulong userId, string weaponString, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return false;
            }

            return this._retakeDB.InsertOrUpdateMidSecondaryWeaponString(userId, weaponString, team);
        }

        public bool InsertOrUpdatePistolWeaponString(ulong userId, string weaponString, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return false;
            }

            return this._retakeDB.InsertOrUpdatePistolWeaponString(userId, weaponString, team);
        }

        public (string? primaryWeapon, string? secondaryWeapon, int? awpChance) GetFullBuyWeapons(ulong userId, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return (null,null,null);
            }

            return this._retakeDB.GetFullBuyWeapons(userId, team);
        }

        public (string? primaryWeapon, string? secondaryWeapon, int? awpChance) GetMidWeapons(ulong userId, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return (null, null, null);
            }

            return this._retakeDB.GetMidWeapons(userId, team);
        }

        public (string? primaryWeapon, string? secondaryWeapon, int? awpChance) GetPistolWeapons(ulong userId, int team)
        {
            if (this.DBType == DBType.Cache || this._retakeDB == null)
            {
                return (null, null, null);
            }

            return this._retakeDB.GetPistolWeapons(userId, team);
        }
    }
}
