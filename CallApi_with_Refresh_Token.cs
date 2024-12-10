private void CallApi(string reviewStatus, long customerId)
        {
            try
            {
                switch (reviewStatus)
                {
                    case "4":
                        reviewStatus = "RELEASED";
                        break;
                    case "3":
                        reviewStatus = "BLOCKED";
                        break;
                    default:
                        reviewStatus = "PENDING";
                        break;
                }

                using (HttpClient http = new HttpClient())
                {
                    _cookie = new ManageCookie();

                    var aibl_onboard_authentication_request_url = ConfigurationManager.AppSettings["aibl_onboard_screening_request_url"] + "/oauth/token";

                    HttpResponseHeaders responseHeaders;

                    string forgeryToken = String.Empty, access_token = String.Empty, refresh_token = String.Empty;

                    dynamic responseContent;

                    var cookieExists = HttpContext.Current.Request.Cookies.Get("AIBL_Onboard_Screening");

                    var refreshTokenExists = HttpContext.Current.Request.Cookies.Get("AIBL_Onboard_Screening_RefreshToken");


                    if (cookieExists != null)
                    {
                        access_token = cookieExists.Values.Get(0).Decrypt();
                        forgeryToken = cookieExists.Values.Get(1).Decrypt();
                    }
                    else if (refreshTokenExists != null && refreshTokenExists.Expires >= DateTime.Now)
                    {
                        refresh_token = refreshTokenExists.Values.Get(0).Decrypt();

                        var requestBody = new
                        {
                            grant_type = "refresh_token",
                            refresh_token = refresh_token
                        };



                        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                        var response = http.PostAsync(aibl_onboard_authentication_request_url, content).GetAwaiter().GetResult();

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            HelperSeriLog.WriteError($"Unable to refresh token {response}");
                            return;
                        }

                        responseHeaders = response.Headers;

                        responseContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                        if (responseHeaders.TryGetValues("F-Token", out var ForgeryTokenValue))
                        {
                            forgeryToken = ForgeryTokenValue.FirstOrDefault();
                        }
                        else
                        {
                            HelperSeriLog.WriteError("Forgery Token not found in refresh token");
                            return;
                        }

                        if (responseContent.access_token != null && responseContent.refresh_token != null)
                        {
                            access_token = responseContent.access_token;
                        }
                        else
                        {
                            HelperSeriLog.WriteError("Access Token Not Found");
                            return;
                        }

                        var aibl = new Dictionary<string, dynamic>()
                        {
                            { "Access_Token", access_token},
                            { "F-Token", forgeryToken }
                        };



                        _cookie.SetCookie("AIBL_Onboard_Screening", aibl, (3600 / (86400 * 1000)));

                    }
                    else
                    {
                        var requestBody = new
                        {
                            grant_type = "Password",
                            username = ConfigurationManager.AppSettings["aibl_onboard_screening_username"],
                            password = ConfigurationManager.AppSettings["aibl_onboard_screening_password"]
                        };

                        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                        var response = http.PostAsync(aibl_onboard_authentication_request_url, content).GetAwaiter().GetResult();

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            HelperSeriLog.WriteError($"Unable to Login {response}");
                            return;
                        }

                        responseHeaders = response.Headers;

                        responseContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                        if (responseHeaders.TryGetValues("F-Token", out var ForgeryTokenValue))
                        {
                            forgeryToken = ForgeryTokenValue.FirstOrDefault();
                        }
                        else
                        {
                            HelperSeriLog.WriteError("Forgery Token not found");
                            return;
                        }

                        if (responseContent.access_token != null && responseContent.refresh_token != null)
                        {
                            access_token = responseContent.access_token;
                            refresh_token = responseContent.refresh_token;
                        }
                        else
                        {
                            HelperSeriLog.WriteError("Access Token Not Found");
                            return;
                        }

                        var aibl = new Dictionary<string, dynamic>()
                            {
                                { "Access_Token", access_token},
                                { "F-Token", forgeryToken }

                            };

                        var aiblRefreshToken = new Dictionary<string, dynamic>()
                        {
                            { "Refresh_Token", refresh_token }
                        };

                        _cookie = new ManageCookie();

                        _cookie.SetCookie("AIBL_Onboard_Screening", aibl, (3600 / (86400 * 1000))); // converting milisecond to days
                        _cookie.SetCookie("AIBL_Onboard_Screening_RefreshToken", aiblRefreshToken, 1);
                    }


                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", access_token);
                    http.DefaultRequestHeaders.Add("F-Token", forgeryToken);

                    var customerInfo = _customer.FirstOrDefault(x => x.Id == customerId);

                    if (reviewStatus == "RELEASED" && customerInfo != null)
                    {
                        var requestBody2 = new
                        {
                            message = "Customer is cleared",
                            requestId = customerInfo.RequestId,
                            status = "CLEARED"
                        };

                        var jsonData2 = JsonConvert.SerializeObject(requestBody2);

                        var content2 = new StringContent(jsonData2, Encoding.UTF8, "application/json");

                        var aibl_onboard_screening_request_url = ConfigurationManager.AppSettings["aibl_onboard_screening_request_url"] + "/aml-screening-service/screening/updatestatus";

                        var result = http.PostAsync(aibl_onboard_screening_request_url, content2).GetAwaiter().GetResult();

                        var responseContent2 = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        
                        if (result.StatusCode != System.Net.HttpStatusCode.OK || result.StatusCode != HttpStatusCode.Unauthorized)
                        {
                            HelperSeriLog.WriteError($"{result.StatusCode}: {responseContent2.ToString()}");
                            return;
                        }
                        HelperSeriLog.WriteInformation($"{result.StatusCode}: {responseContent2.ToString()}");
                    }
                    else
                    {
                        HelperSeriLog.WriteError($"{reviewStatus}, {customerInfo}");
                    }


                }



            }
            catch (Exception ex)
            {
                HelperSeriLog.WriteError(ex);

            }
        }
