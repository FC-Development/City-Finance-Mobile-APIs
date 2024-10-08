﻿using GniApi.Dtos.RequestDto.RequestDto;
using GniApi.Helper;
using GniApi.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Drawing;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GniApi.Controllers
{
    [Route("/api/v1/customers")]
    //[ServiceFilter(typeof(HeaderCheckActionFilter))]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IOracleQueries oracleQueries;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;


        public CustomerController(IOracleQueries oracleQueries, HttpClient httpClient, IConfiguration configuration)
        {
            this.oracleQueries = oracleQueries;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _configuration = configuration;
        }


        [HttpGet("{pin}/limit")]
        public async Task<IActionResult> Limit([FromRoute] string pin = "5ZKX6JW")
        {
            var response = await GetLimit(pin);
            if (response.StatusCode == 200)
                return StatusCode(response.StatusCode, new
                {
                    Limit = response.Item,
                    MainLimitAmount = response.MainLimitAmount
                });


            else
                return StatusCode(response.StatusCode, new
                {
                    ErrorMessage = response.ErrorText
                });
        }

        // TESTED
        [HttpGet("{pin}/payment-history")]
        public IActionResult PaymentHistory([FromRoute(Name = "pin")] string pin = "5ZKX6JW",
                                            string loanId = "801T09230144S01",
                                            int page = 0,
                                            int size = 10,
                                            string sort = "date,ASC")
        {
            var json = JsonSerializer.Serialize(new { pin, loanId, page, sort, size });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_payments", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });


            return Ok(response.Result);

        }

        // needs to be discussed
        // select with PIN
        [HttpGet("{pin}/payment-history/{payment-id}")]
        public IActionResult PaymentHistoryById([FromRoute(Name = "pin")] string pin = "5ZKX6JW", [FromRoute(Name = "payment-id")] string paymentId = "336786",
                                                 string loanId = "801T09230144S01")
        {
            var json = JsonSerializer.Serialize(new { paymentId, loanId });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_payment_info", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });

            return Ok(response.Result);
        }


        [HttpGet("{pin}/loans")]
        public IActionResult Loans([FromRoute(Name = "pin")] string pin = "5ZKX6JW",
                                    string status = "Active",
                                    DateTime? fromDate = null,
                                    DateTime? toDate = null,
                                    int page = 0,
                                    int size = 10,
                                    string sort = "date,ASC")
        {
            var json = JsonSerializer.Serialize(new { pin, status, fromDate = fromDate?.ToString("yyyy-MM-dd"), toDate = toDate?.ToString("yyyy-MM-dd"), page, size, sort });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_contracts", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });

            return Ok(response.Result);
        }



        [HttpGet("{pin}/loans/{loan-id}")]
        public IActionResult LoanContractsById([FromRoute(Name = "pin")] string pin = "5ZKX6JW", [FromRoute(Name = "loan-id")] string loanId = "801T09230144S01")
        {
            var json = JsonSerializer.Serialize(new { loanId });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_contract_info", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });

            return Ok(response.Result);
        }



        [HttpGet("{pin}/loans/{loan-id}/payment-table")]
        public IActionResult PaymentTable([FromRoute(Name = "pin")] string pin = "5ZKX6JW",
                                          [FromRoute(Name = "loan-id")] string loanId = "801T09230144S01")
        {
            var json = JsonSerializer.Serialize(new { loanId });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_payment_plan", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });

            return Ok(response.Result);

        }

        [HttpGet("{pin}/loan-requests")]
        public IActionResult LoanRequests([FromRoute(Name = "pin")] string pin = "15MRAG2",
                                              string status = "Pending",
                                              DateTime? fromDate = null,
                                              DateTime? toDate = null,
                                              int page = 0,
                                              int size = 10,
                                              string sort = "date,ASC")
        {
            var json = JsonSerializer.Serialize(new { pin, status, fromDate = fromDate?.ToString("yyyy-MM-dd"), toDate = toDate?.ToString("yyyy-MM-dd"), page, size, sort });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_requests", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });

            return Ok(response.Result);
        }

        [HttpGet("{pin}/loan-requests/{request-id}")]
        public async Task<IActionResult> LoanRequestById([FromRoute(Name = "pin")] string pin = "15MRAG2", [FromRoute(Name = "request-id")] int requestId = 155)
        {

            var json = JsonSerializer.Serialize(new { requestId, pin });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_request_info", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });

            return Ok(response.Result);
        }


        [HttpPost("loan-requests/create")]
        public async Task<IActionResult> CreateLoanRequest([FromBody] RequestDto model)
        {

            int? limit = (int)(await GetLimit(model.customer.pin)).Item;
            if (limit > model.loanRequest.amount)
            {
                model.loanRequest.limitExceeded = true;
            }

            var json = JsonSerializer.Serialize(model);

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_create_request", new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json }, new string[] { "p_consumer", "p_username", "p_password", "p_data" });


            return Ok(response.Result);
        }

        // has error
        [HttpPost("{pin}/loan-requests/post")]
        public IActionResult PostLoanRequest([FromRoute(Name = "pin")] string pin, int requestId)
        {
            var json = JsonSerializer.Serialize(new { requestId, pin });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_post_request",
                new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json },
                new string[] { "p_consumer", "p_username", "p_password", "p_data" });



            return Ok(response.Result);

        }

        [HttpPost("{pin}/loan-request/offer-approve")]
        public IActionResult PostLoanRequestOfferApprove([FromRoute(Name = "pin")] string pin, int requestId)
        {
            var status = "Offer-approved";
            var json = JsonSerializer.Serialize(new { status, requestId, pin });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_request_action",
                new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json },
                new string[] { "p_consumer", "p_username", "p_password", "p_data" });



            return Ok(response.Result);
        }


        [HttpPost("{pin}/loan-request/offer-reject")]
        public IActionResult PostLoanRequestOfferReject([FromRoute(Name = "pin")] string pin, int requestId)
        {
            var status = "Offer-rejected";
            var json = JsonSerializer.Serialize(new { status, requestId, pin });

            var response = oracleQueries.GetDataSetFromDBFunction("cfmb_loan_request_action",
                new object[] { "MOBILE", "HADINAJAFI", "HADI@12345", json },
                new string[] { "p_consumer", "p_username", "p_password", "p_data" });



            return Ok(response.Result);
        }


        private async Task<ApiResponse> GetLimit(string pin)
        {
            try
            {
                var url = $"{_configuration["Url:LimitUrl"]?.ToString()}?finCode={pin}";

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

                requestMessage.Headers.Add("x-api-key", "city_finance");

                var response = await _httpClient.SendAsync(requestMessage);


                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    //var mainLimitAmount = int.Parse(result.TryGetProperty("mainLimitAmount", out var mainAmountProperty)
                    //    ? mainAmountProperty.ToString() : "0");
                    var limitAmount = int.Parse(result.TryGetProperty("limitAmount", out var limitAmountProperty)
                        ? limitAmountProperty.ToString() : "0");

                    return new()
                    {
                        StatusCode = 200,
                        Item = limitAmount > 0 ? limitAmount : 0,
                        MainLimitAmount = limitAmount > 0 ? limitAmount : 0
                    };
                }
                else
                {

                    return (int)response.StatusCode == 404 ? new ApiResponse()
                    {
                        StatusCode = 200,
                        Item = 0,
                        MainLimitAmount = 0
                    } : new()
                    {
                        StatusCode = (int)response.StatusCode,
                        ErrorText = "Error fetching data from external API"
                    };

                }
            }
            catch (TaskCanceledException)
            {
                return new()
                {
                    StatusCode = 200,
                    Item = 0,
                    MainLimitAmount = 0
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    StatusCode = 500,
                    ErrorText = $"Internal server error: {ex.Message}"
                };
            }

        }
    }
}
