using System.Web;
using System.Web.Optimization;

namespace graph_tutorial
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/4a55c4159e.js",
                        "~/Content/vendors/js/vendor.bundle.base.js",
                        "~/Content/vendors/js/vendor.bundle.addons.js",
                        "~/Scripts/off-canvas.js",
                        "~/Scripts/hoverable-collapse.js",
                        "~/Scripts/template.js",
                        "~/Scripts/settings.js",
                        "~/Scripts/todolist.js",
                        "~/Scripts/dashboard.js",
                        "~/Scripts/todolist.js",
                        "~/Scripts/jquery.datetimepicker.js"
                        ));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/b4").Include(
                       "~/Content/bootstrap.css",
                       "~/Content/animate.css"
                       ));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            //bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
            //            "~/Scripts/modernizr-*"));

            //bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
            //          "~/Scripts/bootstrap.js",
            //          "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/horizontal-layout/style.css",
                      "~/Content/horizontal-layout/font-awesome.min.css"
                      ));
        }
    }
}
