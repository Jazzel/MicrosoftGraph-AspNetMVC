// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using graph_tutorial.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using graph_tutorial.Classes;
using System.Web;
using System.IdentityModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using static System.Net.WebRequestMethods;
using System.Configuration;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Identity;

namespace graph_tutorial.Controllers
{
    public class HomeController : BaseController
    {
        ApplicationDbContext db;
        private readonly HttpClient httpClient;
        private static string graphScopes = ConfigurationManager.AppSettings["ida:AppScopes"];


        public HomeController()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            db = new ApplicationDbContext();
            httpClient = new HttpClient();

        }


        public void UserHeader(ClaimsIdentity userClaims)
        {

            var email = userClaims?.FindFirst("preferred_username")?.Value;
            var user = db.Users.Where(m => m.Email == email).FirstOrDefault();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            ViewBag.name = user.Name;
            ViewBag.role = userManager.GetRoles(user.Id)[0];

        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dashboard()
        {

            var userClaims = User.Identity as ClaimsIdentity;

            var name = userClaims?.FindFirst("name")?.Value;

            var user = db.Users.Where(m => m.Name == name).FirstOrDefault();

            var roleManger = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            if (user == null)
            {
                if (!roleManger.RoleExists("Admin"))
                {
                    var newRole = new IdentityRole();
                    newRole.Name = "Admin";
                    roleManger.Create(newRole);
                }
                var newUser = new ApplicationUser()
                {
                    Email = userClaims?.FindFirst("preferred_username")?.Value,
                    EmailConfirmed = true,
                    DateOfJoining = DateTime.Now,
                    Name = name,
                    UserName = userClaims?.FindFirst("preferred_username")?.Value,
                    Tenant= userClaims?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value,
                    Subject = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    PhoneNumber = userClaims?.FindFirst(ClaimTypes.HomePhone)?.Value,
                };
                var result = userManager.Create(newUser, "*****" + newUser.Email);
                if (result.Succeeded)
                {
                    userManager.AddToRole(newUser.Id, "Admin");
                }
            }
            else
            {
                System.Web.HttpCookie httpCookie = Request.Cookies.Get("logUpdated");
                if (httpCookie.Value != "true")
                {
                    Log log = new Log()
                    {

                        Action = "Login",
                        ApplicationUserId = user.Id,
                        Timestamp = DateTime.Now,
                    };
                    db.Logs.Add(log);
                    db.SaveChanges();
                    httpCookie.Value = "true";
                    Response.Cookies.Add(httpCookie);

                }

            }

            UserHeader(userClaims);


           var  role =  userManager.GetRoles(user.Id)[0];

            if (role != "Teacher")
            {
                return RedirectToAction("TeachersDashboard");
            }
            else
            {
                return View();
            }
        }


        public async Task<ActionResult> TeachersDashboard()
        {
            var userClaims = User.Identity as ClaimsIdentity;

            string API_URL = "https://hulms.instructure.com/api/v1/courses";
            string ACCESS_TOKEN = "17361~ZRl7sKcI03MLwLA3nSBMLtZIJoYgM9Nr4D1ijHBBG1tuo2DNt4iEsnG5faHMJeBK";
            UserHeader(userClaims);

            try
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + ACCESS_TOKEN);

                // Send the HTTP GET request
                HttpResponseMessage response = await httpClient.GetAsync(API_URL);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    string responseData = await response.Content.ReadAsStringAsync();
                    List<object> listData = JsonConvert.DeserializeObject<List<object>>(responseData);

                    // TODO: Process the response data as
                    ViewBag.data = listData;
                    
                    return View(); // Return a view with the processed data
                }
                else
                {
                    // The API returned an error
                    string errorMessage = $"API request failed with status code: {response.StatusCode}";
                    // TODO: Handle the error appropriately, such as displaying an error message

                    return View("Error"); // Return an error view
                }
            }
            catch (Exception ex)
            {
                // An exception occurred while making the request
                string errorMessage = $"An error occurred while making the API request: {ex.Message}";
                // TODO: Handle the exception appropriately, such as logging or displaying an error message

                return View("Error"); // Return an error view
            }
        }
        [HttpPost]
        public ActionResult TeachersDashboard(CourseFile courseFile)
        {
            var userClaims = User.Identity as ClaimsIdentity;
            UserHeader(userClaims);
            var email = userClaims?.FindFirst("preferred_username")?.Value;
            var user = db.Users.Where(m => m.Email == email).FirstOrDefault();


            courseFile.ApplicationUserId = user.Id;
            courseFile.Status = "INITIALIZED";
            courseFile.Term = "Spring";
            courseFile.Title = "Canvas API";
            courseFile.CourseSection = "Section";
            
            db.CourseFiles.Add(courseFile);
            db.SaveChanges();

            return RedirectToAction("Folders", "Home", new { id = courseFile.Id });
        }

        public ActionResult Folders(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseFile courseFile = db.CourseFiles.Find(id);
            if (courseFile == null)
            {
                return HttpNotFound();
            }

            var userClaims = User.Identity as ClaimsIdentity;
            UserHeader(userClaims);

            string rootDir = Server.MapPath("~/Data/");
            string folderPath = Path.Combine(rootDir, courseFile.CourseProgram, courseFile.Title, courseFile.Term, courseFile.CourseSection);
            string syllabusPath = Path.Combine(folderPath, "Course Syllabus");
            string objectivesPath = Path.Combine(folderPath, "Course Objectives");
            string contentPath = Path.Combine(folderPath, "Course Content");
            string weeklyPath = Path.Combine(folderPath, "Weekly Plan of Content of Lectures Delivered");
            string attendancePath = Path.Combine(folderPath, "Attendance Record");
            string lecPath = Path.Combine(folderPath, "Copy of Lecture Notes");
            string refPath = Path.Combine(folderPath, "List of Reference Material");
            string assignPath = Path.Combine(folderPath, "Copy of Assignment & Class Assessments");
            string modelAssignPath = Path.Combine(folderPath, "Model Solution of All Assignment & Class Assessment");
            string threeAssignPath = Path.Combine(folderPath, "Three Sample Graded assignments securing max, min and average marks");
            string quizzesPath = Path.Combine(assignPath, "Copy of all Quizzes");
            string modelQuizzesPath = Path.Combine(assignPath, "Model Solution of All Quizzes");
            string threeQuizzesPath = Path.Combine(folderPath, "Three Sample Graded Quizzes securing max, min and average marks");
            string midPath = Path.Combine(folderPath, "Copy of all midterm exams");
            string modelMidPath = Path.Combine(folderPath, "Model Solution of All mid-term exams");
            string threeMidPath = Path.Combine(folderPath, "Three Sample Mid-Term securing max, min and average marks");
            string finalPath = Path.Combine(folderPath, "Copy of all End Term exams");
            string modelFinalPath = Path.Combine(folderPath, "Model Solution of All Final-term exams");
            string threeFinalPath = Path.Combine(folderPath, "Three Sample End-Term securing max, min and average marks");
            string marksPath = Path.Combine(folderPath, "Marks distribution and grading Model");
            string instructorPath = Path.Combine(folderPath, "Instructor Feedback");
            string resultsPath = Path.Combine(folderPath, "Complete result of the course");
            string obeReportPath = Path.Combine(folderPath, "OBE Report");
            string cqiPath = Path.Combine(folderPath, "CQI");
            string recommendPath = Path.Combine(folderPath, "Recommendations");

            string labOutlinePath = Path.Combine(folderPath, "Lab Outline");
            string labPath = Path.Combine(folderPath, "Lab Manuals");
            string modelLabPath = Path.Combine(folderPath, "Lab Samples");
            string labMidPath = Path.Combine(folderPath, "Copy of all Lab Mid Term Exam");
            string modelLabMidPath = Path.Combine(folderPath, "Model Solution of All Lab Mid Term Exam");
            string threeLabMidPath = Path.Combine(folderPath, "Three Sample Graded All Lab Mid-Term securing max, min and average marks");
            string labFinalPath = Path.Combine(folderPath, "Copy of all End Term exams");
            string modelLabFinalPath = Path.Combine(folderPath, "Model Solution of All Final-term exams");
            string threeLabFinalPath = Path.Combine(folderPath, "Three Sample End-Term securing max, min and average marks");

            string labSyllabusPath = Path.Combine(folderPath, "Lab Syllabus");
            string labObjectivesPath = Path.Combine(folderPath, "Lab Objectives");
            string labContentPath = Path.Combine(folderPath, "Lab Content");
            string weeklyLabPath = Path.Combine(folderPath, "Weekly Plan of Content of Labs Delivered");
            string marksAndGradePath = Path.Combine(folderPath, "Marks distribution and grading Model");
            string outcomesPath = Path.Combine(folderPath, "Outcomes Assessment (OBE Analytics Report)");
            string designPath = Path.Combine(folderPath, "Design Skills Technique");
            string materialPath = Path.Combine(folderPath, "Copy of Material Given");

            string quizzesExamsPath = Path.Combine(folderPath, "Copy of all Quizzes & Exams");
            string modelQuizzesExamsPath = Path.Combine(folderPath, "Model Solution of All Quizzes & Exams");
            string threeQuizzesExamsPath = Path.Combine(folderPath, "Three Sample Graded Quizzes & Exams securing max, min and average marks");
            string evaluationPath = Path.Combine(folderPath, "Student Evaluation");



            if (courseFile.Status == "INITIALIZED")
            {
                System.IO.Directory.CreateDirectory(folderPath);

                if (courseFile.CourseProgram == "CS")
                {
                    if (courseFile.CourseType == "Theory")
                    {
                        System.IO.Directory.CreateDirectory(objectivesPath);
                        System.IO.Directory.CreateDirectory(syllabusPath);
                        System.IO.Directory.CreateDirectory(contentPath);
                        System.IO.Directory.CreateDirectory(weeklyPath);
                        System.IO.Directory.CreateDirectory(attendancePath);
                        System.IO.Directory.CreateDirectory(lecPath);
                        System.IO.Directory.CreateDirectory(refPath);
                        System.IO.Directory.CreateDirectory(assignPath);
                        System.IO.Directory.CreateDirectory(modelAssignPath);
                        System.IO.Directory.CreateDirectory(threeAssignPath);
                        System.IO.Directory.CreateDirectory(quizzesPath);
                        System.IO.Directory.CreateDirectory(modelQuizzesPath);
                        System.IO.Directory.CreateDirectory(threeQuizzesPath);
                        System.IO.Directory.CreateDirectory(midPath);
                        System.IO.Directory.CreateDirectory(modelMidPath);
                        System.IO.Directory.CreateDirectory(threeMidPath);
                        System.IO.Directory.CreateDirectory(finalPath);
                        System.IO.Directory.CreateDirectory(modelFinalPath);
                        System.IO.Directory.CreateDirectory(threeFinalPath);
                        System.IO.Directory.CreateDirectory(marksPath);
                        System.IO.Directory.CreateDirectory(instructorPath);
                        System.IO.Directory.CreateDirectory(resultsPath);
                    }
                    else if (courseFile.CourseType == "Lab")
                    {
                        System.IO.Directory.CreateDirectory(labSyllabusPath);
                        System.IO.Directory.CreateDirectory(labObjectivesPath);
                        System.IO.Directory.CreateDirectory(labContentPath);
                        System.IO.Directory.CreateDirectory(weeklyLabPath);
                        System.IO.Directory.CreateDirectory(attendancePath);
                        System.IO.Directory.CreateDirectory(materialPath);
                        System.IO.Directory.CreateDirectory(refPath);
                        System.IO.Directory.CreateDirectory(assignPath);
                        System.IO.Directory.CreateDirectory(modelAssignPath);
                        System.IO.Directory.CreateDirectory(threeAssignPath);
                        System.IO.Directory.CreateDirectory(marksAndGradePath);
                        System.IO.Directory.CreateDirectory(outcomesPath);
                        System.IO.Directory.CreateDirectory(designPath);
                    }

                }
                else if (courseFile.CourseProgram == "ECE")
                {
                    if (courseFile.CourseType == "Theory")
                    {
                        System.IO.Directory.CreateDirectory(syllabusPath);
                        System.IO.Directory.CreateDirectory(obeReportPath);
                        System.IO.Directory.CreateDirectory(attendancePath);
                        System.IO.Directory.CreateDirectory(lecPath);
                        System.IO.Directory.CreateDirectory(assignPath);
                        System.IO.Directory.CreateDirectory(modelAssignPath);
                        System.IO.Directory.CreateDirectory(threeAssignPath);
                        System.IO.Directory.CreateDirectory(quizzesPath);
                        System.IO.Directory.CreateDirectory(modelQuizzesPath);
                        System.IO.Directory.CreateDirectory(threeQuizzesPath);
                        System.IO.Directory.CreateDirectory(midPath);
                        System.IO.Directory.CreateDirectory(modelMidPath);
                        System.IO.Directory.CreateDirectory(threeMidPath);
                        System.IO.Directory.CreateDirectory(finalPath);
                        System.IO.Directory.CreateDirectory(modelFinalPath);
                        System.IO.Directory.CreateDirectory(threeFinalPath);
                        System.IO.Directory.CreateDirectory(resultsPath);
                        System.IO.Directory.CreateDirectory(cqiPath);
                        System.IO.Directory.CreateDirectory(recommendPath);
                    }
                    else if (courseFile.CourseType == "Lab")
                    {
                        System.IO.Directory.CreateDirectory(labOutlinePath);
                        System.IO.Directory.CreateDirectory(obeReportPath);
                        System.IO.Directory.CreateDirectory(attendancePath);
                        System.IO.Directory.CreateDirectory(labPath);
                        System.IO.Directory.CreateDirectory(modelLabPath);
                        System.IO.Directory.CreateDirectory(labMidPath);
                        System.IO.Directory.CreateDirectory(modelLabMidPath);
                        System.IO.Directory.CreateDirectory(threeLabMidPath);
                        System.IO.Directory.CreateDirectory(labFinalPath);
                        System.IO.Directory.CreateDirectory(modelLabFinalPath);
                        System.IO.Directory.CreateDirectory(threeLabFinalPath);
                        System.IO.Directory.CreateDirectory(resultsPath);
                        System.IO.Directory.CreateDirectory(cqiPath);
                        System.IO.Directory.CreateDirectory(recommendPath);
                    }
                }
                else if (courseFile.CourseProgram == "ISciM")
                {
                    System.IO.Directory.CreateDirectory(syllabusPath);
                    System.IO.Directory.CreateDirectory(obeReportPath);
                    System.IO.Directory.CreateDirectory(attendancePath);
                    System.IO.Directory.CreateDirectory(lecPath);
                    System.IO.Directory.CreateDirectory(assignPath);
                    System.IO.Directory.CreateDirectory(modelAssignPath);
                    System.IO.Directory.CreateDirectory(threeAssignPath);
                    System.IO.Directory.CreateDirectory(quizzesPath);
                    System.IO.Directory.CreateDirectory(modelQuizzesPath);
                    System.IO.Directory.CreateDirectory(threeQuizzesPath);
                    System.IO.Directory.CreateDirectory(midPath);
                    System.IO.Directory.CreateDirectory(modelMidPath);
                    System.IO.Directory.CreateDirectory(threeMidPath);
                    System.IO.Directory.CreateDirectory(finalPath);
                    System.IO.Directory.CreateDirectory(modelFinalPath);
                    System.IO.Directory.CreateDirectory(threeFinalPath);
                    System.IO.Directory.CreateDirectory(resultsPath);
                    System.IO.Directory.CreateDirectory(cqiPath);
                    System.IO.Directory.CreateDirectory(recommendPath);
                }

                else
                {
                    System.IO.Directory.CreateDirectory(syllabusPath);
                    System.IO.Directory.CreateDirectory(lecPath);
                    System.IO.Directory.CreateDirectory(assignPath);
                    System.IO.Directory.CreateDirectory(modelAssignPath);
                    System.IO.Directory.CreateDirectory(threeAssignPath);
                    System.IO.Directory.CreateDirectory(quizzesExamsPath);
                    System.IO.Directory.CreateDirectory(modelQuizzesExamsPath);
                    System.IO.Directory.CreateDirectory(threeQuizzesExamsPath);
                    System.IO.Directory.CreateDirectory(attendancePath);
                    System.IO.Directory.CreateDirectory(resultsPath);
                    System.IO.Directory.CreateDirectory(evaluationPath);

                }
                courseFile.Status = "FETCHED";

                if (ModelState.IsValid)
                {
                    db.Entry(courseFile).State = EntityState.Modified;
                    db.SaveChanges();
                }

            }


            List<FolderInfo> folderInfos = new List<FolderInfo>();

            if (System.IO.Directory.Exists(folderPath))
            {
                string[] subdirectories = System.IO.Directory.GetDirectories(folderPath);
                foreach (string subdirectory in subdirectories)
                {
                    string folderName = Path.GetFileName(subdirectory);
                    string[] files = System.IO.Directory.GetFiles(subdirectory);
                    List<string> fileNames = new List<string>();
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        fileNames.Add(fileName);
                    }
                    FolderInfo folderInfo = new FolderInfo(folderName, fileNames);
                    folderInfos.Add(folderInfo);
                }
            }

            ViewBag.folderInfos = folderInfos;
            return View(courseFile);

        }

        public ActionResult File(string courseId, string folder)
        {
            if (courseId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseFile courseFile = db.CourseFiles.Find(int.Parse(courseId));
            if (courseFile == null)
            {
                return HttpNotFound();
            }
            if (folder == null || folder == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var userClaims = User.Identity as ClaimsIdentity;
            UserHeader(userClaims);

            FolderInfo folderInfo;
            string rootDir = Server.MapPath("~/Data/");
            string folderPath = Path.Combine(rootDir, courseFile.CourseProgram, courseFile.Title, courseFile.Term, courseFile.CourseSection, folder);

            if (System.IO.Directory.Exists(folderPath))
            {
                string[] files = System.IO.Directory.GetFiles(folderPath);
                List<string> fileNames = new List<string>();
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    fileNames.Add(fileName);
                }
                folderInfo = new FolderInfo(folder, fileNames);
                ViewBag.FolderInfo = folderInfo;
            }


            return View(courseFile);
        }

        public ActionResult DeleteFile(string courseId, string folder, string file)
        {
            if (courseId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseFile courseFile = db.CourseFiles.Find(int.Parse(courseId));
            if (courseFile == null)
            {
                return HttpNotFound();
            }
            if (folder == null || folder == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            string rootDir = Server.MapPath("~/Data/");
            string folderPath = Path.Combine(rootDir, courseFile.CourseProgram, courseFile.Title, courseFile.Term, courseFile.CourseSection, folder);

            string fileToDelete = Path.Combine(folderPath, file);
            FileInfo fileObj = new FileInfo(fileToDelete);

            if (fileObj.Exists)
            {
                fileObj.Delete();
            }

            return RedirectToAction("File", new { courseId, folder });
        }

        [HttpPost]
        public ActionResult UploadFile(IEnumerable<HttpPostedFileBase> files, string courseId, string folder)
        {
            if (courseId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseFile courseFile = db.CourseFiles.Find(int.Parse(courseId));
            if (courseFile == null)
            {
                return HttpNotFound();
            }
            if (folder == null || folder == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }


            string rootDir = Server.MapPath("~/Data/");
            string folderPath = Path.Combine(rootDir, courseFile.CourseProgram, courseFile.Title, courseFile.Term, courseFile.CourseSection, folder);

            if (files != null && files.Any())
            {
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);

                        string filePath = Path.Combine(folderPath, fileName);
                        file.SaveAs(filePath);
                    }
                }
            }

            return RedirectToAction("File", new { courseId, folder });

        }

        public ActionResult CreateFolder(string courseId, string folder)
        {
            if (courseId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseFile courseFile = db.CourseFiles.Find(int.Parse(courseId));
            if (courseFile == null)
            {
                return HttpNotFound();
            }
            if (folder == null || folder == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            string rootDir = Server.MapPath("~/Data/");
            string folderPath = Path.Combine(rootDir, courseFile.CourseProgram, courseFile.Title, courseFile.Term, courseFile.CourseSection, folder);

            System.IO.Directory.CreateDirectory(folderPath);


            return RedirectToAction("Folders", new { id = courseId });
        }

        public ActionResult Submit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseFile courseFile = db.CourseFiles.Find(id);
            if (courseFile == null)
            {
                return HttpNotFound();
            }

            string directoryPath = Server.MapPath("~/Data/");

            if (!System.IO.Directory.Exists(directoryPath))
            {
                return Content("Directory not found!");
            }


            courseFile.Status = "SUBMITTED";
            // save coursefile

            // upload on drive


            return RedirectToAction("TeachersDashboard", "Home");
        }

        public async Task<GraphServiceClient> GetGraphServiceClientAsync()
        {
            // Your application/client ID obtained from the Azure portal
            string clientId = "4b0e92a4-ea35-4e8c-bd78-34e7fdbc48d0\r\n";

            // Your client secret obtained from the Azure portal
            string clientSecret = "NTg8Q~GjxXOs3VT2XaSNa2krTJHjlRRlyfvN2cXe";

            // The tenant (directory) ID obtained from the Azure portal
            string tenantId = "67c9003b-5370-443e-b5dc-714bc99e42da";

            // The URL of the Microsoft Graph endpoint
            string graphEndpoint = "https://graph.microsoft.com/v1.0";

            // Create a confidential client application
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            // Acquire an access token
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            AuthenticationResult authResult = await confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync();

            // Create the GraphServiceClient with the access token
            GraphServiceClient graphClient = new GraphServiceClient(graphEndpoint, new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                // Add the access token as a bearer token in the request header
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            }));

            return graphClient;
        }

        public async Task<ActionResult> FilesShow()
        {
            string rootDir = Server.MapPath("~/Data/Test");
            var fileName = "CE 2024 Grid.pdf";
            var filePath = Path.Combine(rootDir, fileName);


            var client = await GetGraphServiceClientAsync();

            var request = client.Me.Drive.Root.Children.Request();

            var results = request.GetAsync().Result;
foreach (var file in results)
{
  Console.WriteLine(file.Id + ": " + file.Name);
}

            //using (Stream stream = new FileStream(filePath, FileMode.Open))
            //{
            //    var uploadSession = client.Me.Drive.Root
            //                                    .ItemWithPath(fileName)
            //                                    .CreateUploadSession()
            //                                    .Request()
            //                                    .PostAsync()
            //                                    .Result;

            //    // create upload task
            //    var maxChunkSize = 320 * 1024;
            //    var largeUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxChunkSize);

            //    // create progress implementation
            //    IProgress<long> uploadProgress = new Progress<long>(uploadBytes =>
            //    {
            //        Console.WriteLine($"Uploaded {uploadBytes} bytes of {stream.Length} bytes");
            //    });

            //    // upload file
            //    UploadResult<DriveItem> uploadResult = largeUploadTask.UploadAsync(uploadProgress).Result;
            //    if (uploadResult.UploadSucceeded)
            //    {
            //        Console.WriteLine("File uploaded to user's OneDrive root folder.");
            //    }

            //}
            return View();
        }

        public ActionResult Error(string message, string debug)
        {
            Flash(message, debug);
            return RedirectToAction("Index");
        }

        public ActionResult Logs()
        {
            var userClaims = User.Identity as ClaimsIdentity;
            UserHeader(userClaims);

            return View(db.Logs.Include(v => v.ApplicationUser));
        }


    }
}