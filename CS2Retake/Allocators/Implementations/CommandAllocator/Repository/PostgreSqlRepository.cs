using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Allocators.Implementations.CommandAllocator.Interfaces;
using CS2Retake.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2Retake.Allocators.Implementations.CommandAllocator.Repository
{
    //One short-lived connection per operation (Npgsql pools them internally via the connection
    //string): makes every call independently usable from the async preference-loading path.
    public class PostgreSqlRepository : IDisposable, IRetakeRepository
    {
        private static readonly string[] WeaponTables = { "FullBuyPrimary", "FullBuySecondary", "MidPrimary", "MidSecondary", "Pistol" };

        private readonly string _connectionString;

        public PostgreSqlRepository(string connectionString)
        {
            this._connectionString = connectionString;
            this.Init();
        }

        private NpgsqlConnection OpenConnection()
        {
            var connection = new NpgsqlConnection(this._connectionString);
            connection.Open();
            return connection;
        }

        public void Dispose()
        {
            NpgsqlConnection.ClearAllPools();
        }

        public void Init()
        {
            try
            {
                using var connection = this.OpenConnection();
                using var cmd = connection.CreateCommand();

                foreach (var table in WeaponTables)
                {
                    cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {table} (UserId VARCHAR(64), WeaponString VARCHAR(255), Team INT)";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = $"CREATE TABLE IF NOT EXISTS FullBuyAWPChance (UserId VARCHAR(64), AWPChance INT, Team INT)";
                cmd.ExecuteNonQuery();

                //Legacy databases may contain duplicate (UserId, Team) rows: keep one of them,
                //then enforce uniqueness so the UPSERT statements below can work.
                foreach (var table in WeaponTables.Append("FullBuyAWPChance"))
                {
                    cmd.CommandText = $"DELETE FROM {table} a USING {table} b WHERE a.ctid < b.ctid AND a.UserId = b.UserId AND a.Team = b.Team";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = $"CREATE UNIQUE INDEX IF NOT EXISTS idx_{table.ToLowerInvariant()}_user_team ON {table} (UserId, Team)";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //Rethrow so DBManager.Init() can fall back to Cache mode instead of leaving a
                //half-initialized PostgreSQL backend that fails on every call.
                throw new InvalidOperationException($"Failed to initialize PostgreSQL database. {ex.Message}", ex);
            }
        }

        public bool InsertOrUpdateFullBuyPrimaryWeaponString(ulong userId, string weaponString, int team) => this.UpsertWeaponString("FullBuyPrimary", userId, weaponString, team);

        public bool InsertOrUpdateFullBuySecondaryWeaponString(ulong userId, string weaponString, int team) => this.UpsertWeaponString("FullBuySecondary", userId, weaponString, team);

        public bool InsertOrUpdateMidPrimaryWeaponString(ulong userId, string weaponString, int team) => this.UpsertWeaponString("MidPrimary", userId, weaponString, team);

        public bool InsertOrUpdateMidSecondaryWeaponString(ulong userId, string weaponString, int team) => this.UpsertWeaponString("MidSecondary", userId, weaponString, team);

        public bool InsertOrUpdatePistolWeaponString(ulong userId, string weaponString, int team) => this.UpsertWeaponString("Pistol", userId, weaponString, team);

        public bool InsertOrUpdateFullBuyAWPChance(ulong userId, int chance, int team)
        {
            try
            {
                using var connection = this.OpenConnection();
                using var cmd = connection.CreateCommand();

                cmd.Parameters.AddWithValue("@id", userId.ToString());
                cmd.Parameters.AddWithValue("@chance", chance);
                cmd.Parameters.AddWithValue("@team", team);

                cmd.CommandText = $"INSERT INTO FullBuyAWPChance (UserId, AWPChance, Team) VALUES (@id, @chance, @team) ON CONFLICT (UserId, Team) DO UPDATE SET AWPChance = EXCLUDED.AWPChance";

                return cmd.ExecuteNonQuery() == 1;
            }
            catch (Exception ex)
            {
                MessageUtils.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex.ToString());
            }

            return false;
        }

        private bool UpsertWeaponString(string tableName, ulong userId, string weaponString, int team)
        {
            try
            {
                using var connection = this.OpenConnection();
                using var cmd = connection.CreateCommand();

                cmd.Parameters.AddWithValue("@id", userId.ToString());
                cmd.Parameters.AddWithValue("@weapon", weaponString);
                cmd.Parameters.AddWithValue("@team", team);

                cmd.CommandText = $"INSERT INTO {tableName} (UserId, WeaponString, Team) VALUES (@id, @weapon, @team) ON CONFLICT (UserId, Team) DO UPDATE SET WeaponString = EXCLUDED.WeaponString";

                return cmd.ExecuteNonQuery() == 1;
            }
            catch (Exception ex)
            {
                MessageUtils.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex.ToString());
            }

            return false;
        }

        public (string? primaryWeapon, string? secondaryWeapon, int? awpChance) GetFullBuyWeapons(ulong userId, int team)
        {
            (string? primaryWeapon, string? secondaryWeapon, int? awpChance) returnValue = (null, null, null);

            try
            {
                returnValue.primaryWeapon = this.QueryWeaponString("FullBuyPrimary", userId, team);
                returnValue.secondaryWeapon = this.QueryWeaponString("FullBuySecondary", userId, team);
                returnValue.awpChance = this.QueryAwpChance(userId, team);
            }
            catch (Exception ex)
            {
                MessageUtils.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex.ToString());
            }

            return returnValue;
        }

        public (string? primaryWeapon, string? secondaryWeapon, int? awpChance) GetMidWeapons(ulong userId, int team)
        {
            (string? primaryWeapon, string? secondaryWeapon, int? awpChance) returnValue = (null, null, 0);

            try
            {
                returnValue.primaryWeapon = this.QueryWeaponString("MidPrimary", userId, team);
                returnValue.secondaryWeapon = this.QueryWeaponString("MidSecondary", userId, team);
            }
            catch (Exception ex)
            {
                MessageUtils.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex.ToString());
            }

            return returnValue;
        }

        public (string? primaryWeapon, string? secondaryWeapon, int? awpChance) GetPistolWeapons(ulong userId, int team)
        {
            (string? primaryWeapon, string? secondaryWeapon, int? awpChance) returnValue = (string.Empty, null, 0);

            try
            {
                returnValue.secondaryWeapon = this.QueryWeaponString("Pistol", userId, team);
            }
            catch (Exception ex)
            {
                MessageUtils.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex.ToString());
            }

            return returnValue;
        }

        //The team filter has to be applied per table. The previous JOIN on UserId only could return the other team's rows.
        private string? QueryWeaponString(string tableName, ulong userId, int team)
        {
            using var connection = this.OpenConnection();
            using var cmd = connection.CreateCommand();

            cmd.Parameters.AddWithValue("@id", userId.ToString());
            cmd.Parameters.AddWithValue("@team", team);

            cmd.CommandText = $"SELECT WeaponString FROM {tableName} WHERE UserId = @id AND Team = @team";

            return cmd.ExecuteScalar() as string;
        }

        private int? QueryAwpChance(ulong userId, int team)
        {
            using var connection = this.OpenConnection();
            using var cmd = connection.CreateCommand();

            cmd.Parameters.AddWithValue("@id", userId.ToString());
            cmd.Parameters.AddWithValue("@team", team);

            cmd.CommandText = $"SELECT AWPChance FROM FullBuyAWPChance WHERE UserId = @id AND Team = @team";

            var result = cmd.ExecuteScalar();

            return result == null || result is DBNull ? null : Convert.ToInt32(result);
        }
    }
}
