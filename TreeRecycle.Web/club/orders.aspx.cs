using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class Orders : ClubPage
    {
        private Dictionary<int, string> _dateNames;

        protected void Page_Load(object sender, EventArgs e)
        {
            var dates = ClubCollectionDate.ForClub(CurrentClub.ClubId, false);
            _dateNames = dates.ToDictionary(d => d.ClubCollectionDateId, d => d.CollectionDate.ToString("ddd d MMM"));
            if (!IsPostBack)
            {
                ddlDate.Items.Add(new ListItem("All days", ""));
                foreach (var d in dates)
                    ddlDate.Items.Add(new ListItem(d.DisplayName, d.ClubCollectionDateId.ToString()));
                if (ddlDate.Items.FindByValue(Q("date")) != null)
                    ddlDate.SelectedValue = Q("date");
                Bind();
            }
        }

        protected string DateName(int id)
        {
            string name;
            return _dateNames.TryGetValue(id, out name) ? name : "";
        }

        protected static string PillClass(TreeOrder o)
        {
            switch (o.StatusEnum)
            {
                case OrderStatus.Paid: return "gold";
                case OrderStatus.Collected: return "";
                case OrderStatus.NotOut: return "red";
                default: return "grey";
            }
        }

        private List<TreeOrder> Filtered()
        {
            IEnumerable<TreeOrder> list = TreeOrder.ForClub(CurrentClub.ClubId).Where(o => o.StatusEnum != OrderStatus.Cancelled);
            if (ddlDate.SelectedValue != "")
                list = list.Where(o => o.ClubCollectionDateId.ToString() == ddlDate.SelectedValue);
            if (ddlStatus.SelectedValue != "")
                list = list.Where(o => o.Status.ToString() == ddlStatus.SelectedValue);
            string s = (txtSearch.Text ?? "").Trim();
            if (s != "")
            {
                string compact = s.Replace(" ", "").ToUpperInvariant();
                list = list.Where(o => o.CustomerName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                                    || o.OrderRef.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                                    || o.Eircode.Replace(" ", "").Contains(compact)
                                    || o.CustomerPhone.Replace(" ", "").Contains(s.Replace(" ", ""))
                                    || o.FullAddress.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            switch (ddlSort.SelectedValue)
            {
                case "distance": list = list.OrderBy(o => o.DistanceKm); break;
                case "eircode": list = list.OrderBy(o => o.Eircode); break;
                case "newest": list = list.OrderByDescending(o => o.CreatedDate); break;
                default: list = list.OrderBy(o => o.RunNumber == 0 ? int.MaxValue : o.RunNumber).ThenBy(o => o.RunSequence).ThenBy(o => o.DistanceKm); break;
            }
            return list.ToList();
        }

        private void Bind()
        {
            var list = Filtered();
            rptOrders.DataSource = list;
            rptOrders.DataBind();
            litSummary.Text = list.Count + " order(s), " + list.Sum(o => o.Quantity) + " tree(s), " + Money(list.Where(o => o.IsSale).Sum(o => o.ClubAmount)) + " to the club.";
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            Bind();
        }

        protected void rptOrders_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            int.TryParse(Convert.ToString(e.CommandArgument), out id);
            TreeOrder o = TreeOrder.FindById(id);
            if (o == null || o.ClubId != CurrentClub.ClubId)
                return;

            switch (e.CommandName)
            {
                case "collected":
                    o.StatusEnum = OrderStatus.Collected;
                    o.CollectedDate = DateTime.UtcNow;
                    o.UpdatedBy = "CLB";
                    o.Update();
                    break;
                case "notout":
                    o.StatusEnum = OrderStatus.NotOut;
                    o.StatusNote = "Not out when the crew called, " + Settings.ToIrish(DateTime.UtcNow).ToString("d MMM HH:mm");
                    o.UpdatedBy = "CLB";
                    o.Update();
                    break;
                case "undo":
                    o.StatusEnum = OrderStatus.Paid;
                    o.CollectedDate = null;
                    o.StatusNote = "";
                    o.UpdatedBy = "CLB";
                    o.Update();
                    break;
                case "resend":
                    litMsg.Text = Mailer.SendOrderEmail(o, EmailTemplateType.Confirmation)
                        ? "<div class=\"msg ok\">Confirmation re-sent to " + Enc(o.CustomerEmail) + ".</div>"
                        : "<div class=\"msg err\">The email couldn't be sent.</div>";
                    break;
                case "refund":
                    try
                    {
                        Payments.Refund(o, CurrentClub);
                        litMsg.Text = "<div class=\"msg ok\">" + Enc(o.OrderRef) + " refunded. The customer will see it in 5-10 days.</div>";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Refund failed for " + o.OrderRef + ": " + ex);
                        litMsg.Text = "<div class=\"msg err\">The refund failed: " + Enc(ex.Message) + "</div>";
                    }
                    break;
            }
            Bind();
        }

        protected void btnCsv_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Run,Stop,Ref,Status,Collection day,Customer,Phone,Email,Address 1,Address 2,Town,Eircode,Latitude,Longitude,Km from club,Tree,Qty,Club amount,Stripe fee,Notes");
            foreach (TreeOrder o in Filtered())
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    o.RunNumber.ToString(), o.RunSequence.ToString(), o.OrderRef, o.StatusText, DateName(o.ClubCollectionDateId),
                    o.CustomerName, o.CustomerPhone, o.CustomerEmail, o.AddressLine1, o.AddressLine2, o.Town, o.Eircode,
                    o.Latitude.ToString(CultureInfo.InvariantCulture), o.Longitude.ToString(CultureInfo.InvariantCulture),
                    o.DistanceKm.ToString("0.0", CultureInfo.InvariantCulture), o.SizeLabel, o.Quantity.ToString(),
                    o.ClubAmount.ToString("0.00", CultureInfo.InvariantCulture), o.StripeFee.ToString("0.00", CultureInfo.InvariantCulture), o.LocationNotes,
                }.Select(Csv)));
            }
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.ContentEncoding = Encoding.UTF8;
            Response.AddHeader("Content-Disposition", "attachment; filename=tree-orders-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".csv");
            Response.BinaryWrite(Encoding.UTF8.GetPreamble());
            Response.Write(sb.ToString());
            Response.Flush();
            Context.ApplicationInstance.CompleteRequest();
            Response.SuppressContent = true;
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            // Neutralise spreadsheet formulas typed into customer fields.
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0)
                value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }
    }
}
