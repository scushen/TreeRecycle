using System.Web.UI;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class SiteMaster : MasterPage
    {
        protected string SiteName { get { return Settings.SiteName; } }
    }
}
