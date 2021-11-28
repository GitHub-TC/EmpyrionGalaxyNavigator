using System.Collections.Generic;
using System.Text;
using System.Data;
using EmpyrionNetAPIDefinitions;
using System;
using System.Data.SQLite;
using System.Linq;

namespace EmpyrionGalaxyNavigator
{
    public class SaveGameDBAccess
    {
        public static Action<string, LogLevel> Log { get; set; } = (m, l) => Console.WriteLine(m);
        public string GlobalDbFilePath { get; }

        public SaveGameDBAccess(string globalDbFilePath)
        {
            GlobalDbFilePath = globalDbFilePath;
        }

        // returns the number of bookmarks added
        public int InsertBookmarks(IEnumerable<NavPoint> positions, int playerFactionId, int playerId, ulong gameTicks)
        {
            SQLiteConnection connection = null;
            SQLiteCommand command = null;

            try
            {
                connection = GetConnection(writeable: true);
                command = connection.CreateCommand();

                // new route if first not exists
                using(var readerCommand = connection.CreateCommand()){
                    readerCommand.CommandText = $"select name from Bookmarks where entityid='{playerId}' and name='NavTo:{positions.First().Name}';";
                    using (var reader = readerCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            command.CommandText = $"delete from Bookmarks where entityid='{playerId}' and name like 'NavTo:%';";
                            command.ExecuteNonQuery();
                        }
                    }
                }

                int bid = GetStartingBookmarkId(command);
                var sql = new StringBuilder(@"insert into Bookmarks ('bid','type','refid','facgroup','facid','entityid','pfid',
                            'name','sectorx','sectory','sectorz','posx','posy','posz','icon','isshared',
                            'iswaypoint','isremove','isshowhud','iscallback','createdticks',
                            'expireafterticks','mindistance','maxdistance') values ");
                int stepNo = 1;
                foreach (var p in positions)
                {
                    sql.Append($"({bid},1,0,1,{playerId},{playerId},{p.PlayfieldId},");
                    sql.Append($"'NavTo:{p.Name}',{p.Coordinates.X:0},{p.Coordinates.Y:0},{p.Coordinates.Z:0},0,0,0,1,0,");
                    sql.Append($"1,1,1,0,");
                    sql.Append($"{gameTicks},0,0,-1),");
                    stepNo++;
                    bid++;
                }
                sql.Replace(',', ';', sql.Length - 1, 1);
                command.CommandText = sql.ToString();
                var success = command.ExecuteNonQuery();

                Log($"Sqlite in InsertBookmarks: {success} -> {command.CommandText}", LogLevel.Debug);

                return success;
            }
            catch (SQLiteException ex)
            {
                Log($"SqliteException in InsertBookmarks: {ex.Message} -> {command.CommandText}", LogLevel.Error);
            }
            finally
            {
                command?.Dispose();
                connection?.Dispose();
            }
            return 0;
        }

        int GetStartingBookmarkId(SQLiteCommand command)
        {
            IDataReader reader = null;
            try
            {
                command.CommandText = "select coalesce(max(bid), 1) from Bookmarks;";
                reader = command.ExecuteReader();
                return reader.Read() ? reader.GetInt32(0) + 1 : 1;
            }
            finally
            {
                reader?.Dispose();
            }
        }

        public int ClearPathMarkers(int playerId)
        {
            SQLiteConnection connection = null;
            SQLiteCommand command = null;

            try
            {
                connection = GetConnection(writeable: true);
                command = connection.CreateCommand();
                command.CommandText = $"delete from Bookmarks where entityid='{playerId}' and name like 'NavTo:%' escape '\\';";
                var success = command.ExecuteNonQuery();
                Log($"Sqlite in ClearPathMarkers: {success} -> {command.CommandText}", LogLevel.Debug);
                return success;
            }
            catch (SQLiteException ex)
            {
                Log($"SqliteException in ClearPathMarkers: {ex.Message} -> {command.CommandText}", LogLevel.Error);
                return 0;
            }
            finally
            {
                command?.Dispose();
                connection?.Dispose();
            }
        }

        public Map GetSolarSystems()
        {
            SQLiteConnection connection = null;
            SQLiteCommand command = null;
            IDataReader reader = null;

            var result = new Map();

            try
            {
                connection = GetConnection();
                command = connection.CreateCommand();
                command.CommandText = "select ssid, name, sectorx, sectory, sectorz from SolarSystems;";
                reader = command.ExecuteReader();
                while(reader.Read()) result.Nodes.Add(reader.GetString(1), new Node() { SolarSystemId = reader.GetInt32(0),  Name = reader.GetString(1), Coordinates = new System.Numerics.Vector3(reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)) });

                Log($"Sqlite in GetSolarSystems: {command.CommandText}", LogLevel.Debug);
            }
            catch (SQLiteException ex)
            {
                Log($"SqliteException in GetSolarSystems: {ex.Message} -> {command.CommandText}", LogLevel.Error);
            }
            finally
            {
                reader?.Dispose();
                command?.Dispose();
                connection?.Dispose();
            }

            return result;
        }

        public Map GetSectorSystems()
        {
            SQLiteConnection connection = null;
            SQLiteCommand command = null;
            IDataReader reader = null;

            var result = new Map();

            try
            {
                connection = GetConnection();
                command = connection.CreateCommand();
                command.CommandText = "select ssid, pfid, name, sectorx, sectory, sectorz from Playfields;";
                reader = command.ExecuteReader();
                while (reader.Read()) result.Nodes.Add(reader.GetString(2), new Node() { 
                    SolarSystemId = reader.GetInt32(0), 
                    PlayfieldId   = reader.GetInt32(1), 
                    Name          = reader.GetString(2), 
                    Coordinates   = new System.Numerics.Vector3(reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5)) 
                });

                Log($"Sqlite in GetSectorSystems: {command.CommandText}", LogLevel.Debug);
            }
            catch (SQLiteException ex)
            {
                Log($"SqliteException in GetSectorSystems: {ex.Message} -> {command.CommandText}", LogLevel.Error);
            }
            finally
            {
                reader?.Dispose();
                command?.Dispose();
                connection?.Dispose();
            }

            return result;
        }

        SQLiteConnection GetConnection(bool writeable = false)
        {
            var details = new SQLiteConnectionStringBuilder();
            details.DataSource = GlobalDbFilePath;
            details.ReadOnly = !writeable;
            var connection = new SQLiteConnection(details.ToString());
            connection.Open();
            return connection;
        }
    }
}
