using Bridge.Html5;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PathIO = System.IO.Path;

namespace Core.Clients
{
    public class Client
    {
        public static DateTime EpsilonNow => DateTime.Now.AddMinutes(2);
        public const string ErrorMessage = "Hệ thống đang cập nhật vui lòng chờ trong 30s!";
        public static string ModelNamespace;
        private readonly string _nameSpace;
        private bool _config;
        public string NameSpace => _nameSpace.IsNullOrEmpty() ? ModelNamespace : _nameSpace;
        public static string Host => Window.Instance["Host"] != null ? Window.Instance["Host"].ToString() : Window.Location.Host;
        public static string Origin => Window.Instance["OriginLocation"] != null ? Window.Instance["OriginLocation"].ToString() : (Window.Location.Origin + "/");
        public static string Prefix => Origin + "api";
        public string CustomPrefix { get; set; } = Document.Head.Children.Where(x => x is HTMLMetaElement).Cast<HTMLMetaElement>().FirstOrDefault(x =>
        {
            return x is HTMLMetaElement meta && meta.Name == "prefix";
        })?.Content;
        public string EntityName { get; set; }
        private static Dictionary<string, Entity> entities;
        private static Token token;
        public static int GuidLength = 36;
        public static string Tenant => Document.Head.Children.Where(x => x is HTMLMetaElement).Cast<HTMLMetaElement>().FirstOrDefault(x =>
        {
            return x is HTMLMetaElement meta && meta.Name == "tenant";
        })?.Content;
        public static string FileFTP => Document.Head.Children.Where(x => x is HTMLMetaElement).Cast<HTMLMetaElement>().FirstOrDefault(x =>
        {
            return x is HTMLMetaElement meta && meta.Name == "file";
        })?.Content;
        public static string Config => Document.Head.Children.Where(x => x is HTMLMetaElement).Cast<HTMLMetaElement>().FirstOrDefault(x =>
        {
            return x is HTMLMetaElement meta && meta.Name == "config";
        })?.Content;
        public static BadGatewayQueue BadGatewayRequest = new BadGatewayQueue();
        private static int _errorMessageAwaiter;

        public static Action<XHRWrapper> UnAuthorizedEventHandler;
        public static Action SignOutEventHandler;

        public string FileName { get; set; }
        public string FileType { get; set; }
        public static Token Token
        {
            get
            {
                if (token != null)
                {
                    return token;
                }

                return LocalStorage.GetItem<Token>("UserInfo");
            }

            set
            {
                token = value;
                LocalStorage.SetItem("UserInfo", value);
            }
        }

        public static Dictionary<string, Entity> Entities
        {
            get
            {
                if (entities != null)
                {
                    return entities;
                }
                return LocalStorage.GetItem<Dictionary<string, Entity>>("Entities");
            }

            set
            {
                entities = value;
                LocalStorage.SetItem("Entities", value);
            }
        }

        public static bool SystemRole { get; set; }

        public static bool CheckHasRole(RoleEnum role)
        {
            if (Token is null)
            {
                return false;
            }

            return Token.RoleNames.Any(x => x.IndexOf(role.ToString().Replace("_", " "), StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool CheckHasRole(IEnumerable<string> roleNames, RoleEnum role)
        {
            return roleNames.Any(x => x.IndexOf(role.ToString().Replace("_", " "), StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool CheckIsRole(string roleName, RoleEnum role)
        {
            return roleName.IndexOf(role.ToString().Replace("_", " "), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string ApplyParameter<T>(T param)
        {
            var propVal = param.GetType().GetProperties().Select(prop => $"{prop.Name}={prop.GetValue(param)}");
            return string.Join("&", propVal);
        }

        public Client()
        {

        }

        public Client(string entityName, string ns = string.Empty, bool config = false)
        {
            _nameSpace = ns;
            _config = config;
            if (_nameSpace.HasAnyChar() && _nameSpace.Last() != '.')
            {
                _nameSpace += '.';
            }
            EntityName = entityName;
        }

        public async Task<T> SubmitAsync<T>(XHRWrapper options)
        {
            if (!options.AllowAnonymous)
            {
                await RefreshToken();
            }
            return await SubmitAsyncWithToken<T>(options);
        }

        public Task<T> Fetch<T>(XHRWrapper options)
        {
            var tcs = new TaskCompletionSource<T>();
            FetchWrapper(options, (x) =>
            {
                tcs.TrySetResult((T)x);
            }, (x) =>
            {
                tcs.TrySetException(new Exception(x.ResponseText));
            });
            return tcs.Task;
        }

        private static void FetchWrapper(XHRWrapper options, Action<object> success, Action<XMLHttpRequest> error)
        {
            var isNotFormData = options.FormData is null;
            var xhr = new XMLHttpRequest();
            if (options.Headers is null && options.FormData is null)
            {
                options.Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                };
            }
            if (options.Url.HasAnyChar() && options.Url[0] == '/')
            {
                options.Url = options.Url.Substring(1);
            }

            if (options.FinalUrl is null)
            {
                var url = options.Url;
                var tenant = Utils.GetUrlParam(Utils.TenantField);
                if (tenant.IsNullOrEmpty())
                {
                    tenant = Tenant;
                }
                if (Utils.GetUrlParam(Utils.TenantField, options.Url).IsNullOrWhiteSpace() && Token is null && options.AddTenant)
                {
                    var tenantQuery = "t=" + (tenant ?? "wr1");
                    url += url.Contains(Utils.QuestionMark) ? "&" + tenantQuery : (Utils.QuestionMark + tenantQuery);
                }
                options.FinalUrl = Window.EncodeURI(System.IO.Path.Combine(options.Prefix ?? Prefix, options.EntityName, url));
            }
            xhr.Open(options.Method.ToString(), options.FinalUrl, true);
            options.Headers.SelectForeach(x => xhr.SetRequestHeader(x.Key, x.Value));
            if (!options.AllowAnonymous)
            {
                xhr.SetRequestHeader(Utils.Authorization, "Bearer " + Token?.AccessToken);
            }

            xhr.OnReadyStateChange += () =>
            {
                if (xhr.ReadyState != AjaxReadyState.Done)
                {
                    return;
                }

                if (xhr.Status >= (int)HttpStatusCode.OK && xhr.Status < (int)HttpStatusCode.MultipleChoices)
                {
                    var json = JSON.Parse(xhr.ResponseText);
                    success.Call(null, json, xhr);
                }
                else
                {
                    error.Call(null, xhr);
                }
            };
            if (options.ProgressHandler != null)
            {
                xhr.AddEventListener(EventType.Progress, options.ProgressHandler);
            }
            if (isNotFormData)
            {
                xhr.Send(options.JsonData);
            }
            else
            {
                xhr.Send(options.FormData);
            }
        }

        private Task<T> SubmitAsyncWithToken<T>(XHRWrapper options)
        {
            CustomPrefix = _config ? Config : CustomPrefix;
            var isNotFormData = options.FormData is null;
            var tcs = new TaskCompletionSource<T>();
            var xhr = new XMLHttpRequest();
            if (options.Headers is null && options.FormData is null)
            {
                options.Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                };
            }
            if (options.Url.HasAnyChar() && options.Url[0] == '/')
            {
                options.Url = options.Url.Substring(1);
            }

            if (options.FinalUrl is null)
            {
                var url = options.Url;
                var tenant = Utils.GetUrlParam(Utils.TenantField);
                if (tenant.IsNullOrEmpty())
                {
                    tenant = Tenant;
                }
                if (Utils.GetUrlParam(Utils.TenantField, options.Url).IsNullOrWhiteSpace() && Token is null && options.AddTenant)
                {
                    var tenantQuery = "t=" + (tenant ?? "wr1");
                    url += url.Contains(Utils.QuestionMark) ? "&" + tenantQuery : (Utils.QuestionMark + tenantQuery);
                }
                options.FinalUrl = Window.EncodeURI(PathIO.Combine(CustomPrefix ?? Prefix, EntityName, url));
            }
            xhr.Open(options.Method.ToString(), options.FinalUrl, true);
            options.Headers.SelectForeach(x => xhr.SetRequestHeader(x.Key, x.Value));
            if (!options.AllowAnonymous)
            {
                xhr.SetRequestHeader(Utils.Authorization, "Bearer " + Token?.AccessToken);
            }

            xhr.OnReadyStateChange += () =>
            {
                if (xhr.ReadyState != AjaxReadyState.Done)
                {
                    return;
                }

                if (xhr.Status >= (int)HttpStatusCode.OK && xhr.Status < (int)HttpStatusCode.MultipleChoices)
                {
                    ProcessSuccessRequest(options, tcs, xhr);
                }
                else
                {
                    ErrorHandler(options, tcs, xhr);
                }
            };
            if (options.ProgressHandler != null)
            {
                xhr.AddEventListener(EventType.Progress, options.ProgressHandler);
            }
            if (isNotFormData)
            {
                xhr.Send(options.JsonData);
            }
            else
            {
                xhr.Send(options.FormData);
            }
            return tcs.Task;
        }

        private static void ErrorHandler<T>(XHRWrapper options, TaskCompletionSource<T> tcs, XMLHttpRequest xhr)
        {
            if (options.Retry)
            {
                tcs.TrySetResult(false.As<T>());
                return;
            }
            TmpException exp;
            try
            {
                exp = JsonConvert.DeserializeObject<TmpException>(xhr.ResponseText);
                exp.StatusCode = (HttpStatusCode)xhr.Status;
            }
            catch
            {
                exp = new TmpException { Message = "Đã có lỗi xảy ra trong quá trình xử lý", StackTrace = xhr.ResponseText };
            }
            if (options.ErrorHandler != null)
            {
                options.ErrorHandler.Invoke(xhr);
                tcs.TrySetException(new HttpException(exp.Message) { XHR = xhr });
                return;
            }
            if (xhr.Status >= (int)HttpStatusCode.BadRequest && xhr.Status < (int)HttpStatusCode.InternalServerError)
            {
                if (exp != null && !exp.Message.IsNullOrWhiteSpace() && options.ShowError)
                {
                    Toast.Warning(exp.Message);
                }
                Console.WriteLine(exp);
            }
            else if (xhr.Status == (int)HttpStatusCode.InternalServerError || xhr.Status == (int)HttpStatusCode.NotFound)
            {
                Console.WriteLine(exp);
            }
            else if (xhr.Status == (int)HttpStatusCode.Unauthorized)
            {
                UnAuthorizedEventHandler?.Invoke(options);
            }
            else if (xhr.Status >= (int)HttpStatusCode.BadGateway || xhr.Status == (int)HttpStatusCode.GatewayTimeout || xhr.Status == (int)HttpStatusCode.ServiceUnavailable)
            {
                if (options.ShowError)
                {
                    Toast.Warning("Lỗi kết nối tới máy chủ, vui lòng chờ trong giây lát...");
                }
                if (!options.Retry)
                {
                    BadGatewayRequest.Enqueue(options);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(xhr.ResponseText))
                {
                    Toast.Warning(xhr.ResponseText);
                }
            }
            tcs.TrySetException(new HttpException(exp.Message) { XHR = xhr });
        }

        private static void ProcessSuccessRequest<T>(XHRWrapper options, TaskCompletionSource<T> tcs, XMLHttpRequest xhr)
        {
            if (options.Retry)
            {
                tcs.TrySetResult(true.As<T>());
                return;
            }
            if (xhr.ResponseText.IsNullOrEmpty())
            {
                tcs.TrySetResult(default(T));
                return;
            }
            if (options.CustomParser != null)
            {
                tcs.TrySetResult(options.CustomParser(xhr.Response).As<T>());
                return;
            }
            var type = typeof(T);
            if (type.IsInt32())
            {
                tcs.TrySetResult(xhr.ResponseText.TryParseInt().As<T>());
            }
            else if (type.IsDecimal())
            {
                tcs.TrySetResult(xhr.ResponseText.TryParseDecimal().As<T>());
            }
            else if (typeof(T) == typeof(string))
            {
                tcs.TrySetResult(xhr.ResponseText.As<T>());
            }
            else if (typeof(T) == typeof(Blob))
            {
                Blob result = null;
                /*@
                var blob = new Blob([xhr.response], xhr.responseType);
                */
                tcs.TrySetResult(result.As<T>());
            }
            else
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<T>(xhr.ResponseText);
                    tcs.TrySetResult(parsed);
                }
                catch
                {
                    object jsonT = JSON.Parse(xhr.ResponseText);
                    tcs.TrySetResult(Convert.ChangeType(jsonT, typeof(T)).As<T>());
                }
            }
        }

        public async Task<OdataResult<T>> GetList<T>(string filter = null, bool clearCache = false) where T : class
        {
            var headers = ClearCacheHeader(clearCache);
            var xhr = new XHRWrapper
            {
                Value = null,
                Url = filter,
                Headers = headers,
                Method = HttpMethod.GET
            };

            if (typeof(T) != typeof(object))
            {
                var type = typeof(T);
                EntityName = type?.Name;
                return await SubmitAsync<OdataResult<T>>(xhr);
            }
            var refType = Type.GetType(NameSpace + EntityName);
            if (refType == null)
            {
                return await SubmitAsync<OdataResult<object>>(xhr).As<Task<OdataResult<T>>>();
            }
            var request = GetType().GetMethods().FirstOrDefault(x => x.Name == nameof(GetList) && x.IsGenericMethodDefinition);
            if (request is null)
            {
                return new OdataResult<T>();
            }

            return await request.MakeGenericMethod(refType).Invoke(this, filter, clearCache).As<Task<OdataResult<T>>>();
        }

        public async Task<List<T>> GetRawList<T>(string filter = null, bool clearCache = false, bool addTenant = false, bool annonymous = false, string entityName = null) where T : class
        {
            EntityName = entityName ?? typeof(T).Name;
            var headers = ClearCacheHeader(clearCache);
            var res = await SubmitAsync<OdataResult<T>>(new XHRWrapper
            {
                Value = null,
                AddTenant = addTenant,
                Url = filter,
                Headers = headers,
                AllowAnonymous = annonymous,
                Method = HttpMethod.GET
            });
            return res?.Value;
        }

        private static Dictionary<string, string> ClearCacheHeader(bool clearCache)
        {
            var headers = new Dictionary<string, string>();
            if (clearCache)
            {
                headers.Add("Pragma", "no-cache");
                headers.Add("Expires", "0");
                headers.Add("Last-Modified", new DateTime().ToString());
                headers.Add("If-Modified-Since", new DateTime().ToString());
                headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
            }

            return headers;
        }

        public async Task<T> FirstOrDefaultAsync<T>(string filter = null, bool clearCache = false, bool addTenant = false) where T : class
        {
            filter = OdataExt.ApplyClause(filter, 1.ToString(), OdataExt.TopKeyword);
            EntityName = typeof(T).Name;
            var headers = ClearCacheHeader(clearCache);
            var res = await SubmitAsync<OdataResult<T>>(new XHRWrapper
            {
                Value = null,
                AddTenant = addTenant,
                Url = filter,
                Headers = headers,
                Method = HttpMethod.GET
            });
            return res?.Value?.FirstOrDefault();
        }

        public async Task<object> FirstOrDefaultAsync(Type type, string filter = null, bool clearCache = false)
        {
            filter = OdataExt.ApplyClause(filter, 1.ToString(), OdataExt.TopKeyword);
            EntityName = type.Name;
            var headers = ClearCacheHeader(clearCache);
            var response = await SubmitAsync<OdataResult<object>>(new XHRWrapper
            {
                Value = null,
                Url = filter,
                Headers = headers,
                Method = HttpMethod.GET
            });
            var res = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(response.Value.FirstOrDefault()), type);
            return res;
        }

        public Task<OdataResult<object>> LoadById(string listId, string tenant = string.Empty, string action = "ById")
        {
            return SubmitAsync<OdataResult<object>>(new XHRWrapper
            {
                Value = listId,
                IsRawString = true,
                Method = HttpMethod.POST,
                Url = action,
                Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                }
            });
        }

        public async Task<List<T>> GetRawListById<T>(List<string> listId, string tenant = string.Empty, string action = string.Empty, bool clearCache = false) where T : class
        {
            if (listId.Nothing())
            {
                return new List<T>();
            }
            var listIdStr = listId.Distinct().Select(x => $"'{x}'").Combine(",");
            var refType = Type.GetType(NameSpace + EntityName);
            var httpGetList = GetType().GetMethods()
                .FirstOrDefault(x => x.Name == nameof(GetRawList) && x.IsGenericMethodDefinition);
            if (httpGetList is null)
            {
                return new List<T>();
            }
            var filter = $"{action}/?{(tenant != string.Empty ? $"t={tenant}&" : string.Empty)}$filter=Id in ({listIdStr})";
            return await httpGetList.MakeGenericMethod(refType).Invoke(this, filter, clearCache).As<Task<List<T>>>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "<Pending>")]
        public async Task<T> GetByIdAsync<T>(string id)
        {
            if (id.IsNullOrEmpty())
            {
                return default(T);
            }
            EntityName = typeof(T).Name;
            var odata = await SubmitAsync<OdataResult<T>>(new XHRWrapper
            {
                Value = id,
                IsRawString = true,
                Method = HttpMethod.POST,
                Url = "ById",
                Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                }
            });
            return odata.Value.FirstOrDefault();
        }

        public Task<object> GetRawAsync(string id)
        {
            var refType = Type.GetType(NameSpace + EntityName);
            var get = GetType().GetMethods()
                .FirstOrDefault(x => x.Name == nameof(GetAsync) && x.IsGenericMethodDefinition);
            if (get is null)
            {
                return null;
            }

            return get.MakeGenericMethod(refType).Invoke(this, id).As<Task<object>>();
        }

        /// <summary>
        /// This method is used when we don't have return type at compiled time
        /// </summary>
        /// <param name="value"></param>
        /// <param name="subUrl"></param>
        /// <returns></returns>
        public Task<object> CreateAsync(object value, string subUrl = string.Empty)
        {
            var refType = Type.GetType(NameSpace + EntityName);
            var httpGetList = GetType().GetMethods()
                .FirstOrDefault(x => x.Name == nameof(PostAsync) && x.IsGenericMethodDefinition);
            if (httpGetList is null)
            {
                return Task.FromResult(new object());
            }

            return httpGetList.MakeGenericMethod(refType).Invoke(this, value, subUrl).As<Task<object>>();
        }

        public Task<T> CreateAsync<T>(object value, string subUrl = string.Empty)
        {
            return PostAsync<T>(value, subUrl);
        }

        public Task<T> PostAsync<T>(object value, string subUrl = string.Empty, bool annonymous = false, bool allowNested = false)
        {
            return SubmitAsync<T>(new XHRWrapper
            {
                Value = value,
                Url = subUrl,
                Method = HttpMethod.POST,
                AllowAnonymous = annonymous,
            });
        }

        public Task<T> PutAsync<T>(object value, string subUrl = string.Empty, bool annonymous = false, bool allowNested = false)
        {
            return SubmitAsync<T>(new XHRWrapper
            {
                Value = value,
                Url = subUrl,
                Method = HttpMethod.PUT,
                AllowAnonymous = annonymous,
            });
        }

        public Task<T> GetAsync<T>(string subUrl)
        {
            return SubmitAsync<T>(new XHRWrapper
            {
                Url = subUrl,
                Method = HttpMethod.GET
            });
        }

        /// <summary>
        /// This method is used when we don't have return type at compiled time
        /// </summary>
        /// <param name="value"></param>
        /// <param name="subUrl"></param>
        /// <returns></returns>
        public Task<dynamic> UpdateAsync(object value, string subUrl = string.Empty)
        {
            var refType = Type.GetType((NameSpace ?? "Core.Models.") + EntityName);
            var httpGetList = GetType().GetMethods()
                .FirstOrDefault(x => x.Name == nameof(UpdateAsync) && x.IsGenericMethodDefinition);
            if (httpGetList is null)
            {
                return Task.FromResult(new object());
            }

            return httpGetList.MakeGenericMethod(refType).Invoke(this, value, subUrl).ToDynamic();
        }

        public Task<T> UpdateAsync<T>(object value, string subUrl = string.Empty, bool annonymous = false, bool allowNested = false)
        {
            return SubmitAsync<T>(new XHRWrapper
            {
                Value = value,
                Url = subUrl,
                Method = HttpMethod.PUT,
                AllowAnonymous = annonymous,
            });
        }

        public Task<T> PatchAsync<T>(PatchUpdate value, string subUrl = string.Empty, string ig = string.Empty, bool annonymous = false, bool allowNested = false)
        {
            var id = value.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
            return SubmitAsync<T>(new XHRWrapper
            {
                Value = JsonConvert.SerializeObject(value),
                IsRawString = true,
                Url = subUrl + $"?$filter=Id eq '{id.Value}'{ig}",
                Headers = new Dictionary<string, string> { { "Content-type", "application/json" } },
                Method = HttpMethod.PATCH,
                AllowAnonymous = annonymous,
            });
        }

        public Task<T> PostFilesAsync<T>(File file, string url = string.Empty, Action<object> progressHandler = null)
        {
            var formData = new FormData();
            formData.Append("file", file);
            CustomPrefix = FileFTP;
            return SubmitAsync<T>(new XHRWrapper
            {
                FormData = formData,
                File = file,
                ProgressHandler = progressHandler,
                Method = HttpMethod.POST,
                Url = url
            });
        }

        public Task<bool> SendMail(EmailVM email)
        {
            return SubmitAsync<bool>(new XHRWrapper
            {
                Value = email,
                Method = HttpMethod.POST,
                Url = "Email"
            });
        }
        public Task<bool> CloneFeatureAsync(string id)
        {
            return SubmitAsync<bool>(new XHRWrapper
            {
                Url = "Clone",
                Value = id,
                IsRawString = true,
                Method = HttpMethod.POST
            });
        }
        public Task<bool> DeactivateAsync(List<string> ids)
        {
            return SubmitAsync<bool>(new XHRWrapper
            {
                Url = "Delete",
                Value = ids.Combine(),
                Method = HttpMethod.DELETE
            });
        }

        public async Task<List<T>> BulkUpdateAsync<T>(List<T> value, string reasonOfChange = string.Empty, bool multipleThread = true)
        {
            if (!multipleThread)
            {
                return await SubmitAsync<List<T>>(new XHRWrapper
                {
                    Url = $"BulkUpdate/?reasonOfChange={reasonOfChange}",
                    Value = value,
                    Method = HttpMethod.PUT
                });
            }

            var tasks = value.Select(x =>
            {
                if (x[Utils.IdField].As<int>() > 0)
                {
                    return UpdateAsync<T>(x, $"?reasonOfChange={reasonOfChange}");
                }
                else
                {
                    return CreateAsync<T>(x, $"?reasonOfChange=Thêm mới dữ liệu");
                }
            });
            var res = await Task.WhenAll(tasks);
            return res.ToList();
        }

        public async Task<List<T>> PatchAsync<T>(List<PatchUpdate> value, string reasonOfChange = string.Empty, bool multipleThread = true)
        {
            var tasks = value.Select(x => PatchAsync<T>(x));
            var res = new List<T>();
            foreach (var item in value)
            {
                res.Add(await PatchAsync<T>(item));
            }
            return res;
        }

        public Task<bool> HardDeleteAsync(string id) => HardDeleteAsync(new List<string> { id });

        public Task<bool> HardDeleteAsync(List<string> ids)
        {
            return SubmitAsync<bool>(new XHRWrapper
            {
                Url = "HardDelete",
                Value = JsonConvert.SerializeObject(ids),
                Method = HttpMethod.DELETE,
                IsRawString = true,
                Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                }
        });
        }

        public static Task<bool> LoadScript(string src)
        {
            var tcs = new TaskCompletionSource<bool>();
            var scriptExists = Document.Body.Children
                .Where(x => x is HTMLScriptElement)
                .Cast<HTMLScriptElement>()
                .Any(x => x.Src.Split("/").LastOrDefault() == src.Split("/").LastOrDefault());
            if (scriptExists)
            {
                tcs.SetResult(true);
                return tcs.Task;
            }
            var script = Document.CreateElement(ElementType.Script.ToString()).As<HTMLScriptElement>();
            script.Src = src;
            script.OnLoad += (Event<HTMLScriptElement> e) =>
            {
                tcs.SetResult(true);
            };
            script.OnError += (string message, string url, int lineNumber, int columnNumber, object error) =>
            {
                tcs.SetResult(true);
                return false;
            };
            Document.Body.AppendChild(script);
            return tcs.Task;
        }

        public static Task<bool> LoadLink(string src)
        {
            var tcs = new TaskCompletionSource<bool>();
            var scriptExists = Document.Head.Children
                .Any(x => x is HTMLLinkElement styleElement && styleElement.Href.Replace(Document.Location.Origin, string.Empty) == src);
            if (scriptExists)
            {
                tcs.SetResult(true);
                return tcs.Task;
            }
            var link = Document.CreateElement(ElementType.Style.ToString()).As<HTMLLinkElement>();
            link.Href = src;
            link.OnLoad += (Event<HTMLLinkElement> e) =>
            {
                tcs.SetResult(true);
            };
            link.OnError += (string message, string url, int lineNumber, int columnNumber, object error) =>
            {
                tcs.SetResult(true);
                return false;
            };
            Document.Head.AppendChild(link);
            return tcs.Task;
        }

        public static async Task<Token> RefreshToken(Action<Token> success = null)
        {
            Token oldToken = Token;
            if (oldToken is null || oldToken.RefreshTokenExp <= EpsilonNow)
            {
                return null;
            }
            if (oldToken.AccessTokenExp > EpsilonNow)
            {
                return oldToken;
            }
            if (oldToken.AccessTokenExp <= EpsilonNow && oldToken.RefreshTokenExp > EpsilonNow)
            {
                var newToken = await GetToken(oldToken);
                if (newToken != null)
                {
                    Token = newToken;
                    if (success != null)
                    {
                        success.Invoke(newToken);
                    }
                }
            }
            return null;
        }

        public static async Task<Token> GetToken(Token oldToken)
        {
            var client = new Client(nameof(User), typeof(User).Namespace);
            var newToken = await client.SubmitAsync<Token>(new XHRWrapper
            {
                NoQueue = true,
                Url = $"Refresh?t={Token.TenantCode ?? Tenant}",
                Method = HttpMethod.POST,
                Value = new RefreshVM { AccessToken = oldToken.AccessToken, RefreshToken = oldToken.RefreshToken },
                AllowAnonymous = true,
                ErrorHandler = (xhr) =>
                {
                    if (xhr.Status == (ushort)HttpStatusCode.BadRequest)
                    {
                        Token = null;
                        Toast.Warning("Phiên truy cập đã hết hạn! Vui lòng chờ trong giây lát, hệ thống đang tải lại trang");
                        Window.Location.Reload();
                    }
                },
            });
            return newToken;
        }

        public static void Download(string path)
        {
            var removePath = RemoveGuid(path);
            var a = new HTMLAnchorElement
            {
                Href = path.Contains("http") ? path : System.IO.Path.Combine(Origin, path),
                Target = "_blank"
            };
            a.SetAttribute("download", removePath);
            Document.Body.AppendChild(a);
            a.Click();
            Document.Body.RemoveChild(a);
        }

        public static async Task LoadEntities()
        {
            var entities = await new Client(nameof(Entity), typeof(Entity).Namespace).GetRawList<Entity>(addTenant: true);
            Entities = entities.ToDictionary(x => x.Id);
        }

        internal static string RemoveGuid(string path)
        {
            string thumbText = path;
            if (path.Length > GuidLength)
            {
                var fileName = PathIO.GetFileNameWithoutExtension(path);
                thumbText = fileName.SubStrIndex(0, fileName.Length - GuidLength) + PathIO.GetExtension(path);
            }
            return thumbText;
        }

        public static IPromise ToPromise<T>(Task<T> task)
        {
            if (task == null) return null;
            /*@
            return new Promise((resolve, reject) => {
            var $step = 0,
                $task1, 
                $taskResult1, 
                $jumpFromFinally, 
                $returnValue, 
                t, 
                $async_e, 
                $asyncBody = Bridge.fn.bind(this, function () {
                    try {
                        for (;;) {
                            $step = System.Array.min([0,1], $step);
                            switch ($step) {
                                case 0: {
                                    if (task == null) {
                                        resolve(null);
                                        return;
                                    }
                                    $task1 = task;
                                    $step = 1;
                                    if ($task1.isCompleted()) {
                                        continue;
                                    }
                                    $task1.continue($asyncBody);
                                    return;
                                }
                                case 1: {
                                    $taskResult1 = $task1.getAwaitedResult();
                                    t = $taskResult1;
                                    resolve(t);
                                    return;
                                }
                                default: {
                                    resolve(null);
                                    return;
                                }
                            }
                        }
                    } catch($async_e1) {
                        $async_e = System.Exception.create($async_e1);
                        reject($async_e);
                    }
                }, arguments);

            $asyncBody();
            });
            */
            return null;
        }

        public static IPromise ToPromiseNoResult(Task task)
        {
            if (task == null) return null;
            /*@
            return new Promise((resolve, reject) => {
            var $step = 0,
                $task1, 
                $taskResult1, 
                $jumpFromFinally, 
                $returnValue, 
                t, 
                $async_e, 
                $asyncBody = Bridge.fn.bind(this, function () {
                    try {
                        for (;;) {
                            $step = System.Array.min([0,1], $step);
                            switch ($step) {
                                case 0: {
                                    if (task == null) {
                                        resolve(null);
                                        return;
                                    }
                                    $task1 = task;
                                    $step = 1;
                                    if ($task1.isCompleted()) {
                                        continue;
                                    }
                                    $task1.continue($asyncBody);
                                    return;
                                }
                                case 1: {
                                    $taskResult1 = $task1.getAwaitedResult();
                                    t = $taskResult1;
                                    resolve(t);
                                    return;
                                }
                                default: {
                                    resolve(null);
                                    return;
                                }
                            }
                        }
                    } catch($async_e1) {
                        $async_e = System.Exception.create($async_e1);
                        reject($async_e);
                    }
                }, arguments);

            $asyncBody();
            });
            */
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        public static void ExecTask<T>(Task<T> task, Action<T> handler = null)
        {
            var promise = ToPromise(task);
            /*@
            promise.then(handler);
             */
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        public static void ExecTaskNoResult(Task task, Action handler = null)
        {
            var promise = ToPromiseNoResult(task);
            /*@
            promise.then(handler);
             */
        }
    }
}
