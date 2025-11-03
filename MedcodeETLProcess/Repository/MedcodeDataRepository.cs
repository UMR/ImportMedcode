using Dapper;
using MedcodeETLProcess.Configurations;
using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Model.MedcodeModel;
using System.Data.SqlClient;

namespace MedcodeETLProcess.Repository
{
    public class MedcodeDataRepository(DapperDbContext dbContext) : IMedcodeRepository<MedcodeDataMainModel>
    {
        private readonly DapperDbContext _dbContext = dbContext;

        public async Task<int> AddOrUpdateMedcodeData(List<MedcodeDataMainModel> medcodeData, string CodeVersion)
        {
            var version = !string.IsNullOrEmpty(CodeVersion) ? Convert.ToInt32(CodeVersion) : 2013;
            int recordsProcessed = 0;

            try
            {
                using (var connection = _dbContext.CreateConnection() as SqlConnection)
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var allCodes = medcodeData.Select(d => d.MedicalCode.MedCode.ToLower()).Distinct().ToList();
                            var existingCodesInDB = await connection.QueryAsync<string>(
                                "SELECT MEDCODE FROM [dbo].[UMR_MEDCODES] WHERE LOWER(MEDCODE) IN @Codes",
                                new { Codes = allCodes },
                                transaction);
                            var existingCodesSet = new HashSet<string>(existingCodesInDB.Select(c => c.ToLower()));

                            var insertBatch = medcodeData.Where(d => !existingCodesSet.Contains(d.MedicalCode.MedCode.ToString().ToLower())).ToList();
                            var updateBatch = medcodeData.Where(d => existingCodesSet.Contains(d.MedicalCode.MedCode.ToString().ToLower())).ToList();

                            Console.WriteLine($"[BULK OPERATION] Insert batch: {insertBatch.Count}, Update batch: {updateBatch.Count}");

                            if (insertBatch.Count != 0)
                            {
                                await BulkInsertMedcodes(connection, transaction, insertBatch, version);
                                recordsProcessed += insertBatch.Count;
                            }

                            if (updateBatch.Count != 0)
                            {
                                await BulkUpdateMedcodes(connection, transaction, updateBatch, version);
                                recordsProcessed += updateBatch.Count;
                            }

                            transaction.Commit();
                            Console.WriteLine($"[SUCCESS] Processed {recordsProcessed} records");
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                return recordsProcessed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddOrUpdateMedcodeData: {ex.Message}");
                throw;
            }
        }

        private async Task BulkInsertMedcodes(SqlConnection connection, SqlTransaction transaction, List<MedcodeDataMainModel> data, int codeVersion)
        {
            var medcodesSql = @"INSERT INTO [dbo].[UMR_MEDCODES]
               ([GUID],[CODE_TYPE],[MEDCODE],[SEX_TYPE],[CODE_VERSION],[DETAIL],[MEDICAL_CONTENT_INDEX],
                [MEDCODE_VALUE],[MEDCODE_STATUS],[OASIS_DEMOGRAPHICS],[SECONDARY_STATUS],[IS_CODING_COMPLETED],
                [FUTURE_VERSION],[OWNER_ID],[COMMENT],[IS_RECYCLED_EVER],[PLACEMENT_HOLDER],[TRIGGER],[MISCELLANEOUS],[NO_METRIC])
            VALUES
               (@Guid,@CodeType,@MedCode,@SexType,@CodeVersion,@Detail,@MedicalContentIndex,
                @MedCodeValue,@MedCodeStatus,@OasisDemographics,@SecondaryStatus,@IsCodingCompleted,
                @FutureVersion,@OwnerId,@Comment,@IsRecycledEver,@PlacementHolder,@Trigger,@Miscellaneous,@NoMetric)";

            var medcodesParams = data.Select(d => new
            {
                Guid = d.MedicalCode.Guid,
                CodeType = d.MedicalCode.CodeType.ToString(),
                MedCode = d.MedicalCode.MedCode,
                SexType = "B",
                CodeVersion = codeVersion,
                Detail = d.MedicalCode.Detail,
                MedicalContentIndex = d.MedicalCode.MedicalContentIndex,
                MedCodeValue = d.MedicalCode.MedCodeValue,
                MedCodeStatus = "N",
                OasisDemographics = (string)null,
                SecondaryStatus = (string)null,
                IsCodingCompleted = d.MedicalCode.IsCodingCompleted,
                FutureVersion = (string)null,
                OwnerId = (string)null,
                Comment = (string)null,
                IsRecycledEver = d.MedicalCode.IsRecycledEver,
                PlacementHolder = (string)null,
                Trigger = (string)null,
                Miscellaneous = (string)null,
                NoMetric = d.MedicalCode.NoMetric
            });

            await connection.ExecuteAsync(medcodesSql, medcodesParams, transaction);

            var actionsSql = @"INSERT INTO [dbo].[UMR_HISTORY_MEDCODEACTION]
               ([CODE_TYPE],[MEDCODE],[CODE_VERSION],[ACTION],[TIME])
            VALUES (@CodeType,@MedCode,@CodeVersion,@Action,@Time)";

            var actionsParams = data.Select(d => new
            {
                CodeType = d.MedicalCodeActionHistory.CodeType ?? d.MedicalCode.CodeType,
                MedCode = d.MedicalCodeActionHistory.MedCode,
                CodeVersion = codeVersion,
                Action = d.MedicalCodeActionHistory.Action,
                Time = d.MedicalCodeActionHistory.Time
            });

            await connection.ExecuteAsync(actionsSql, actionsParams, transaction);

            var historySql = @"INSERT INTO [dbo].[UMR_HISTORY_USER_MEDCODES]
               ([USERID],[DATE],[GUID],[MEDCODE],[CODE_TYPE],[ACTION],[CODE_VERSION],[OLD_CODE_VERSION],
                [UMED_SYS_STAT],[COMMENTS],[IS_CODING_COMPLETED])
            VALUES (@UserId,@Date,@Guid,@MedCode,@CodeType,@Action,@CodeVersion,@OldCodeVersion,
                    @UmedSysStat,@Comments,@IsCodingCompleted)";

            var historyParams = data.Select(d => new
            {
                UserId = d.MedicalCodeHistory.UserId,
                Date = d.MedicalCodeHistory.Date,
                Guid = d.MedicalCode.Guid,
                MedCode = d.MedicalCodeHistory.MedCode,
                CodeType = d.MedicalCodeHistory.CodeType,
                Action = d.MedicalCodeHistory.Action,
                CodeVersion = d.MedicalCodeHistory.CodeVersion,
                OldCodeVersion = d.MedicalCodeHistory.OldCodeVersion,
                UmedSysStat = "Created",
                Comments = "Newly created unfinished Medcode.",
                IsCodingCompleted = d.MedicalCodeHistory.IsCodingCompleted
            });

            await connection.ExecuteAsync(historySql, historyParams, transaction);

            var medcodes = data.Select(d => d.MedicalCode.MedCode).ToList();
            var insertedHistoryRecords = await connection.QueryAsync<dynamic>(
                @"SELECT h.ID, h.MEDCODE, h.GUID 
                  FROM (
                      SELECT ID, MEDCODE, GUID, 
                             ROW_NUMBER() OVER (PARTITION BY MEDCODE ORDER BY ID DESC) AS RowNum
                      FROM [dbo].[UMR_HISTORY_USER_MEDCODES]
                      WHERE MEDCODE IN @Codes
                  ) h
                  WHERE h.RowNum = 1",
                new { Codes = medcodes },
                transaction);
            var historyLookup = insertedHistoryRecords.ToDictionary(r => (string)r.MEDCODE, r => new { ID = (int)r.ID, GUID = (Guid)r.GUID });
            var actionHistorySql = @"INSERT INTO [dbo].[UMR_HISTORY_USER_MEDCODES_ACTIONS]
               ([USERID],[DATE],[GUID],[ACTION],[UMR_HISTORY_USER_MEDCODES_ID])
            VALUES (@UserId,@Date,@Guid,@Action,@UmrHistoryUserMedcodesId)";

            var actionHistoryParams = data.Select(d => new
            {
                UserId = d.MedicalCodesAction.UserId,
                Date = d.MedicalCodesAction.Date,
                Guid = d.MedicalCodesAction.Guid ?? d.MedicalCode.Guid,
                Action = d.MedicalCodesAction.Action,
                UmrHistoryUserMedcodesId = historyLookup[d.MedicalCode.MedCode].ID
            }).ToList();

            await connection.ExecuteAsync(actionHistorySql, actionHistoryParams, transaction);

            if (data.Any(d => d.MedcodepediaHistoryDBMedcode != null))
            {
                using (var historyConnection = _dbContext.CreateHistoryConnection() as SqlConnection)
                {
                    historyConnection.Open();
                    using (var historyTransaction = historyConnection.BeginTransaction())
                    {
                        try
                        {
                            var mhMedcodeSql = @"INSERT INTO [dbo].[MHMEDCODE]
                               ([UMR_HISTORY_USER_MEDCODES_ID],[GUID],[CODE_TYPE],[MEDCODE],[SEX_TYPE],[CODE_VERSION],
                                [DETAIL],[MEDICAL_CONTENT_INDEX],[MEDCODE_VALUE],[MEDCODE_STATUS],[OASIS_DEMOGRAPHICS],
                                [SECONDARY_STATUS],[IS_CODING_COMPLETED],[PLACEMENT_HOLDER],[TRIGGER],[MISCELLANEOUS])
                            VALUES (@UmrHistoryUserMedcodesId,@Guid,@CodeType,@MedCode,@SexType,@CodeVersion,
                                    @Detail,@MedicalContentIndex,@MedCodeValue,@MedCodeStatus,@OasisDemographics,
                                    @SecondaryStatus,@IsCodingCompleted,@PlacementHolder,@Trigger,@Miscellaneous)";

                            var mhMedcodeParams = data
                                .Where(d => d.MedcodepediaHistoryDBMedcode != null)
                                .Select(d => new
                                {
                                    UmrHistoryUserMedcodesId = historyLookup[d.MedicalCode.MedCode].ID,
                                    Guid = d.MedicalCode.Guid,
                                    CodeType = d.MedcodepediaHistoryDBMedcode.CodeType ?? d.MedicalCode.CodeType,
                                    MedCode = d.MedcodepediaHistoryDBMedcode.MedCode ?? d.MedicalCode.MedCode,
                                    SexType = d.MedcodepediaHistoryDBMedcode.SexType ?? "B",
                                    CodeVersion = d.MedcodepediaHistoryDBMedcode.CodeVersion ?? codeVersion.ToString(),
                                    Detail = d.MedcodepediaHistoryDBMedcode.Detail ?? d.MedicalCode.Detail,
                                    MedicalContentIndex = d.MedcodepediaHistoryDBMedcode.MedicalContentIndex ?? d.MedicalCode.MedicalContentIndex,
                                    MedCodeValue = d.MedcodepediaHistoryDBMedcode.MedCodeValue ?? d.MedicalCode.MedCodeValue,
                                    MedCodeStatus = d.MedcodepediaHistoryDBMedcode.MedCodeStatus ?? "N",
                                    OasisDemographics = d.MedcodepediaHistoryDBMedcode.OasisDemographics,
                                    SecondaryStatus = d.MedcodepediaHistoryDBMedcode.SecondaryStatus,
                                    IsCodingCompleted = d.MedcodepediaHistoryDBMedcode.IsCodingCompleted ?? d.MedicalCode.IsCodingCompleted,
                                    PlacementHolder = d.MedcodepediaHistoryDBMedcode.PlacementHolder,
                                    Trigger = d.MedcodepediaHistoryDBMedcode.Trigger,
                                    Miscellaneous = d.MedcodepediaHistoryDBMedcode.Miscellaneous
                                }).ToList();

                            await historyConnection.ExecuteAsync(mhMedcodeSql, mhMedcodeParams, historyTransaction);
                            historyTransaction.Commit();
                            Console.WriteLine($"[MHMEDCODE INSERT] Inserted {mhMedcodeParams.Count} NEW records to MedcodepediaHistory database");
                        }
                        catch (Exception ex)
                        {
                            historyTransaction.Rollback();
                            Console.WriteLine($"[MHMEDCODE ERROR] Failed to insert into MedcodepediaHistory: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
        }

        private async Task BulkUpdateMedcodes(SqlConnection connection, SqlTransaction transaction, List<MedcodeDataMainModel> data, int codeVersion)
        {
            var codes = data.Select(d => d.MedicalCode.MedCode).Distinct().ToList();
            var existingRecords = await connection.QueryAsync<MedicalCode>(
                "SELECT GUID, MEDCODE, CODE_VERSION, MEDCODE_STATUS FROM [dbo].[UMR_MEDCODES] WHERE MEDCODE IN @Codes",
                new { Codes = codes },
                transaction);

            var existingDict = existingRecords
                .GroupBy(r => r.MedCode)
                .ToDictionary(g => g.Key, g => g.First())
                .ToDictionary(kvp => kvp.Key, kvp => new { Guid = kvp.Value.Guid, CodeVersion = kvp.Value.CodeVersion, CurrentStatus = kvp.Value.MedCodeStatus });

            var recordsToUpdate = data.Where(d =>
                existingDict.ContainsKey(d.MedicalCode.MedCode) &&
                d.MedicalCode.MedCodeStatus?.ToUpper() != "A"
            ).ToList();

            if (recordsToUpdate.Count != 0)
            {
                var updateSql = @"UPDATE [dbo].[UMR_MEDCODES]
                   SET CODE_VERSION = @CodeVersion,
                       MEDCODE_STATUS = @MedCodeStatus,
                       SECONDARY_STATUS = @SecondaryStatus,
                       DETAIL = @Detail,
                       MEDICAL_CONTENT_INDEX = @MedicalContentIndex
                   WHERE MEDCODE = @MedCode";

                var updateParams = recordsToUpdate.Select(d =>
                {
                    var currentStatus = existingDict[d.MedicalCode.MedCode].CurrentStatus;
                    var excelStatus = d.MedicalCode.MedCodeStatus?.ToUpper();
                    string finalStatus;
                    string secondaryStatus = null;

                    switch (excelStatus)
                    {
                        case "N":
                            finalStatus = "Z";
                            secondaryStatus = currentStatus;
                            break;
                        case "D":
                            finalStatus = "D";
                            break;
                        case "M":
                            finalStatus = "M";
                            break;
                        case "O":
                            finalStatus = "O";
                            break;
                        case "A":
                            finalStatus = currentStatus;
                            break;
                        default:
                            finalStatus = d.MedicalCode.MedCodeStatus;
                            break;
                    }

                    return new
                    {
                        CodeVersion = codeVersion,
                        MedCodeStatus = finalStatus,
                        SecondaryStatus = secondaryStatus,
                        Detail = d.MedicalCode.Detail,
                        MedicalContentIndex = d.MedicalCode.MedicalContentIndex,
                        MedCode = d.MedicalCode.MedCode
                    };
                });

                await connection.ExecuteAsync(updateSql, updateParams, transaction);
                Console.WriteLine($"[UPDATE] Updated {recordsToUpdate.Count} medcodes (skipped {data.Count - recordsToUpdate.Count} with status 'A')");
            }
            else
            {
                Console.WriteLine($"[UPDATE] No medcodes to update (all have status 'A' or not found)");
            }

            var recordsForHistory = data.Where(d =>
                existingDict.ContainsKey(d.MedicalCode.MedCode) &&
                d.MedicalCode.MedCodeStatus?.ToUpper() != "A"
            ).ToList();

            if (recordsForHistory.Any())
            {
                var uniqueMedcodes = recordsForHistory.Select(d => d.MedicalCode.MedCode).Distinct().ToList();

                var previousValuesSql = @"SELECT GUID, MEDCODE, CODE_TYPE, SEX_TYPE, CODE_VERSION, DETAIL, 
                                         MEDICAL_CONTENT_INDEX, MEDCODE_VALUE, MEDCODE_STATUS, OASIS_DEMOGRAPHICS,
                                         SECONDARY_STATUS, IS_CODING_COMPLETED, PLACEMENT_HOLDER, [TRIGGER], MISCELLANEOUS
                                  FROM [dbo].[UMR_MEDCODES] WHERE MEDCODE IN @Codes";

                var previousRecords = await connection.QueryAsync<dynamic>(
                    previousValuesSql,
                    new { Codes = uniqueMedcodes },
                    transaction);

                var previousValuesDict = previousRecords
                    .GroupBy(r => (string)r.MEDCODE)
                    .ToDictionary(g => g.Key, g => g.First());

                var actionsSql = @"INSERT INTO [dbo].[UMR_HISTORY_MEDCODEACTION]
                   ([CODE_TYPE],[MEDCODE],[CODE_VERSION],[ACTION],[TIME])
                VALUES (@CodeType,@MedCode,@CodeVersion,@Action,@Time)";

                var actionsParams = recordsForHistory.Select(d => new
                {
                    CodeType = d.MedicalCodeActionHistory.CodeType ?? d.MedicalCode.CodeType,
                    MedCode = d.MedicalCodeActionHistory.MedCode,
                    CodeVersion = codeVersion,
                    Action = d.MedicalCodeActionHistory.Action,
                    Time = d.MedicalCodeActionHistory.Time
                }).ToList();

                await connection.ExecuteAsync(actionsSql, actionsParams, transaction);

                var historySql = @"INSERT INTO [dbo].[UMR_HISTORY_USER_MEDCODES]
                   ([USERID],[DATE],[GUID],[MEDCODE],[CODE_TYPE],[ACTION],[CODE_VERSION],[OLD_CODE_VERSION],
                    [UMED_SYS_STAT],[COMMENTS],[IS_CODING_COMPLETED])
                VALUES (@UserId,@Date,@Guid,@MedCode,@CodeType,@Action,@CodeVersion,@OldCodeVersion,
                        @UmedSysStat,@Comments,@IsCodingCompleted)";

                var historyParams = recordsForHistory.Select(d => new
                {
                    UserId = d.MedicalCodeHistory.UserId,
                    Date = d.MedicalCodeHistory.Date,
                    Guid = existingDict[d.MedicalCode.MedCode].Guid,
                    MedCode = d.MedicalCodeHistory.MedCode,
                    CodeType = d.MedicalCodeHistory.CodeType ?? d.MedicalCode.CodeType,
                    Action = d.MedicalCodeHistory.Action,
                    CodeVersion = codeVersion,
                    OldCodeVersion = existingDict[d.MedicalCode.MedCode].CodeVersion,
                    UmedSysStat = "Edited",
                    Comments = (string)null,
                    IsCodingCompleted = d.MedicalCodeHistory.IsCodingCompleted
                });

                await connection.ExecuteAsync(historySql, historyParams, transaction);

                var medcodes = recordsForHistory.Select(d => d.MedicalCode.MedCode).ToList();
                var insertedHistoryRecords = await connection.QueryAsync<dynamic>(
                    @"SELECT h.ID, h.MEDCODE, h.GUID 
                      FROM (
                          SELECT ID, MEDCODE, GUID, 
                                 ROW_NUMBER() OVER (PARTITION BY MEDCODE ORDER BY ID DESC) AS RowNum
                          FROM [dbo].[UMR_HISTORY_USER_MEDCODES]
                          WHERE MEDCODE IN @Codes
                      ) h
                      WHERE h.RowNum = 1",
                    new { Codes = medcodes },
                    transaction);
                var historyLookup = insertedHistoryRecords.ToDictionary(r => (string)r.MEDCODE, r => new { ID = (int)r.ID, GUID = (Guid)r.GUID });

                var actionHistorySql = @"INSERT INTO [dbo].[UMR_HISTORY_USER_MEDCODES_ACTIONS]
                   ([USERID],[DATE],[GUID],[ACTION],[UMR_HISTORY_USER_MEDCODES_ID])
                VALUES (@UserId,@Date,@Guid,@Action,@UmrHistoryUserMedcodesId)";

                var actionHistoryParams = recordsForHistory.Select(d => new
                {
                    UserId = d.MedicalCodesAction.UserId,
                    Date = d.MedicalCodesAction.Date,
                    Guid = existingDict[d.MedicalCode.MedCode].Guid,
                    Action = d.MedicalCodesAction.Action,
                    UmrHistoryUserMedcodesId = historyLookup[d.MedicalCode.MedCode].ID
                }).ToList();

                await connection.ExecuteAsync(actionHistorySql, actionHistoryParams, transaction);

                if (previousValuesDict.Any())
                {
                    using (var historyConnection = _dbContext.CreateHistoryConnection() as SqlConnection)
                    {
                        historyConnection.Open();
                        using (var historyTransaction = historyConnection.BeginTransaction())
                        {
                            try
                            {
                                var mhMedcodeSql = @"INSERT INTO [dbo].[MHMEDCODE]
                                   ([UMR_HISTORY_USER_MEDCODES_ID],[GUID],[CODE_TYPE],[MEDCODE],[SEX_TYPE],[CODE_VERSION],
                                    [DETAIL],[MEDICAL_CONTENT_INDEX],[MEDCODE_VALUE],[MEDCODE_STATUS],[OASIS_DEMOGRAPHICS],
                                    [SECONDARY_STATUS],[IS_CODING_COMPLETED],[PLACEMENT_HOLDER],[TRIGGER],[MISCELLANEOUS])
                                VALUES (@UmrHistoryUserMedcodesId,@Guid,@CodeType,@MedCode,@SexType,@CodeVersion,
                                        @Detail,@MedicalContentIndex,@MedCodeValue,@MedCodeStatus,@OasisDemographics,
                                        @SecondaryStatus,@IsCodingCompleted,@PlacementHolder,@Trigger,@Miscellaneous)";

                                var mhMedcodeParams = recordsForHistory
                                    .Where(d => previousValuesDict.ContainsKey(d.MedicalCode.MedCode))
                                    .Select(d =>
                                    {
                                        var oldValues = previousValuesDict[d.MedicalCode.MedCode];
                                        return new
                                        {
                                            UmrHistoryUserMedcodesId = historyLookup[d.MedicalCode.MedCode].ID,
                                            Guid = (Guid)oldValues.GUID,
                                            CodeType = (string)oldValues.CODE_TYPE,
                                            MedCode = (string)oldValues.MEDCODE,
                                            SexType = (string)oldValues.SEX_TYPE ?? "B",
                                            CodeVersion = oldValues.CODE_VERSION?.ToString() ?? codeVersion.ToString(),
                                            Detail = (string)oldValues.DETAIL,
                                            MedicalContentIndex = (string)oldValues.MEDICAL_CONTENT_INDEX,
                                            MedCodeValue = (string)oldValues.MEDCODE_VALUE,
                                            MedCodeStatus = (string)oldValues.MEDCODE_STATUS,
                                            OasisDemographics = (string)oldValues.OASIS_DEMOGRAPHICS,
                                            SecondaryStatus = (string)oldValues.SECONDARY_STATUS,
                                            IsCodingCompleted = (bool?)oldValues.IS_CODING_COMPLETED,
                                            PlacementHolder = (string)oldValues.PLACEMENT_HOLDER,
                                            Trigger = (string)oldValues.TRIGGER,
                                            Miscellaneous = (string)oldValues.MISCELLANEOUS
                                        };
                                    }).ToList();

                                await historyConnection.ExecuteAsync(mhMedcodeSql, mhMedcodeParams, historyTransaction);
                                historyTransaction.Commit();
                                Console.WriteLine($"[MHMEDCODE UPDATE] Inserted {mhMedcodeParams.Count} PREVIOUS records to MedcodepediaHistory database");
                            }
                            catch (Exception ex)
                            {
                                historyTransaction.Rollback();
                                Console.WriteLine($"[MHMEDCODE ERROR] Failed to insert into MedcodepediaHistory: {ex.Message}");
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public async Task<MedicalCode> GetMedcodeByCode(string code)
        {
            try
            {
                using (var connection = _dbContext.CreateConnection())
                {
                    var query = "SELECT * FROM dbo.UMR_MEDCODES WHERE MEDCODE = @Code";
                    var result = await connection.QueryFirstOrDefaultAsync<MedicalCode>(query, new { Code = code });
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMedcodeByCode: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> MedcodeExists(string code)
        {
            try
            {
                using (var connection = _dbContext.CreateConnection())
                {
                    var query = "SELECT Count(1)  FROM dbo.UMR_MEDCODES WHERE MEDCODE = @Code";
                    var count = await connection.ExecuteScalarAsync<int>(query, new { Code = code });
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MedcodeExists: {ex.Message}");
                throw;
            }
        }

        public async Task<HashSet<string>> GetExistingCodes(List<string> codes)
        {
            try
            {
                using (var connection = _dbContext.CreateConnection())
                {
                    var query = "SELECT MEDCODE FROM dbo.UMR_MEDCODES WHERE MEDCODE IN @Codes";
                    var result = await connection.QueryAsync<string>(query, new { Codes = codes });
                    return [.. result];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetExistingCodes: {ex.Message}");
                throw;
            }
        }
    }
}
