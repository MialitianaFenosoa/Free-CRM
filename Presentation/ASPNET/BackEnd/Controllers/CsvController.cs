using Application.Features.CSVManager.Commands;
using Application.Features.CSVManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ASPNET.BackEnd.Controllers
{
    [Route("api/[controller]")]
    public class CsvController : BaseApiController
    {
        public CsvController(ISender sender) : base(sender)
        {
        }

        [AllowAnonymous]
        [HttpGet("Entities")]
        public async Task<ActionResult<ApiSuccessResult<GetCsvEntitiesResult>>> GetCsvEntitiesAsync(CancellationToken cancellationToken)
        {
            var request = new GetCsvEntitiesRequest();
            var response = await _sender.Send(request, cancellationToken);

            return Ok(new ApiSuccessResult<GetCsvEntitiesResult>
            {
                Code = StatusCodes.Status200OK,
                Message = $"Success executing {nameof(GetCsvEntitiesAsync)}",
                Content = response
            });
        }

        // [AllowAnonymous]
        // [HttpPost("Import")]
        // public async Task<ActionResult<ApiSuccessResult<ProcessCsvResult>>> ImportCsvAsync(
        //     [FromForm] IFormFile file,
        //     [FromForm] string entityName,
        //     [FromForm] string separator,
        //     [FromForm] string columnMappings,
        //     CancellationToken cancellationToken)
        // {
        //     if (file == null || file.Length == 0)
        //     {
        //         return BadRequest(new ApiErrorResult
        //         {
        //             Code = StatusCodes.Status400BadRequest,
        //             Message = "Csv file is required."
        //         });
        //     }
        //
        //     try
        //     {
        //         var tempFilePath = Path.GetTempFileName();
        //         using (var stream = new FileStream(tempFilePath, FileMode.Create))
        //         {
        //             await file.CopyToAsync(stream);
        //         }
        //
        //         var request = new ImportCsvRequest
        //         {
        //             FilePath = tempFilePath,
        //             EntityName = entityName,
        //             Separator = separator,
        //         };
        //
        //         var response = await _sender.Send(request, cancellationToken);
        //
        //         if (response.Message.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        //         {
        //             return StatusCode(StatusCodes.Status400BadRequest, new ApiErrorResult
        //             {
        //                 Code = StatusCodes.Status400BadRequest,
        //                 Message = response.Message
        //             });
        //         }
        //
        //         return Ok(new ApiSuccessResult<ProcessCsvResult>
        //         {
        //             Code = StatusCodes.Status200OK,
        //             Message = "Import Successful",
        //             Content = response
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResult
        //         {
        //             Code = StatusCodes.Status500InternalServerError,
        //             Message = $"Error while importing the CSV: {ex.Message}"
        //         });
        //     }
        // }

        
        [Authorize]
        [HttpPost("Import")]
        public async Task<ActionResult<ApiSuccessResult<ProcessCsvResult>>> ImportCsvAsync(
            [FromForm] List<IFormFile> files,  
            [FromForm] string separator,  
            [FromForm] string createdById,  
            CancellationToken cancellationToken)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new ApiErrorResult
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "At least one CSV file is required."
                });
            }

            try
            {
                List<string> tempFilePaths = new(); // Temp paths
                List<string> originalFileNames = new(); // Original file names

                foreach (var file in files)
                {
                    var tempFilePath = Path.GetTempFileName();
                    
                    // Enregistrer le fichier temporaire
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    // Ajouter le chemin temporaire et le nom original
                    tempFilePaths.Add(tempFilePath);
                    originalFileNames.Add(file.FileName); // Récupération du nom original
                }

                var request = new ProcessCsvRequest
                {
                    FilePath = tempFilePaths,
                    Separator = separator,
                    CreatedById = createdById,
                    OriginalFileNames = originalFileNames // Transmettre les noms originaux
                };

                var response = await _sender.Send(request, cancellationToken);

                if (response.Message.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new ApiErrorResult
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Message = response.Message
                    });
                }

                return Ok(new ApiSuccessResult<ProcessCsvResult>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Import Successful",
                    Content = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResult
                {
                    Code = StatusCodes.Status500InternalServerError,
                    Message = $"Error while importing the CSV: {ex.Message}"
                });
            }
        }



        [AllowAnonymous]
        [HttpGet("Export")]
        public async Task<ActionResult<ApiSuccessResult<ExportCsvResult>>> ExportCsvAsync(
            [FromHeader] string entityName,
            [FromHeader] string filePath,
            [FromHeader] string separator = ",",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new ExportCsvRequest
                {
                    EntityName = entityName,
                    Separator = separator
                };

                var response = await _sender.Send(request, cancellationToken);

                if (response.Message.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiErrorResult
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Message = response.Message
                    });
                }
 
                return Ok(new ApiSuccessResult<ExportCsvResult>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Export Successful",
                    Content = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResult
                {
                    Code = StatusCodes.Status500InternalServerError,
                    Message = $"Error while exporting the CSV: {ex.Message}"
                });
            }
        }
    }
}
