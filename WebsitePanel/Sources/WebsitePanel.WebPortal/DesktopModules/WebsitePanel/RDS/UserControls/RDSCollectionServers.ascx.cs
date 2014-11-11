// Copyright (c) 2014, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebsitePanel.Providers.HostedSolution;
using System.Linq;
using WebsitePanel.Providers.Web;
using WebsitePanel.EnterpriseServer.Base.HostedSolution;
using WebsitePanel.Providers.RemoteDesktopServices;

namespace WebsitePanel.Portal.RDS.UserControls
{
    public partial class RDSCollectionServers : WebsitePanelControlBase
	{
        public const string DirectionString = "DirectionString";

		protected enum SelectedState
		{
			All,
			Selected,
			Unselected
		}

        public void SetServers(RdsServer[] servers)
		{
            BindServers(servers, false);
		}

        //public RdsServer[] GetServers()
        //{
        //    return GetGridViewServers(SelectedState.All).ToArray();
        //}

        public List<RdsServer> GetServers()
        {
            return GetGridViewServers(SelectedState.All);
        }

		protected void Page_Load(object sender, EventArgs e)
		{
			// register javascript
			if (!Page.ClientScript.IsClientScriptBlockRegistered("SelectAllCheckboxes"))
			{
				string script = @"    function SelectAllCheckboxes(box)
                {
		            var state = box.checked;
                    var elm = box.parentElement.parentElement.parentElement.parentElement.getElementsByTagName(""INPUT"");
                    for(i = 0; i < elm.length; i++)
                        if(elm[i].type == ""checkbox"" && elm[i].id != box.id && elm[i].checked != state && !elm[i].disabled)
		                    elm[i].checked = state;
                }";
                Page.ClientScript.RegisterClientScriptBlock(typeof(RDSCollectionUsers), "SelectAllCheckboxes",
					script, true);
			}
		}

		protected void btnAdd_Click(object sender, EventArgs e)
		{
			// bind all servers
			BindPopupServers();

			// show modal
			AddServersModal.Show();
		}

		protected void btnDelete_Click(object sender, EventArgs e)
		{
            List<RdsServer> selectedServers = GetGridViewServers(SelectedState.Unselected);

            BindServers(selectedServers.ToArray(), false);
		}

		protected void btnAddSelected_Click(object sender, EventArgs e)
		{
            List<RdsServer> selectedServers = GetPopUpGridViewServers();

            BindServers(selectedServers.ToArray(), true);

		}

        protected void BindPopupServers()
		{
            RdsServer[] servers = ES.Services.RDS.GetOrganizationFreeRdsServersPaged(PanelRequest.ItemID, "FqdName", txtSearchValue.Text, null, 0, 1000).Servers;

            servers = servers.Where(x => !GetServers().Select(p => p.Id).Contains(x.Id)).ToArray();
            Array.Sort(servers, CompareAccount);
            if (Direction == SortDirection.Ascending)
            {
                Array.Reverse(servers);
                Direction = SortDirection.Descending;
            }
            else
                Direction = SortDirection.Ascending;

            gvPopupServers.DataSource = servers;
            gvPopupServers.DataBind();
		}

        protected void BindServers(RdsServer[] newServers, bool preserveExisting)
		{
			// get binded addresses
            List<RdsServer> servers = new List<RdsServer>();
			if(preserveExisting)
                servers.AddRange(GetGridViewServers(SelectedState.All));

            // add new servers
            if (newServers != null)
			{
                foreach (RdsServer newServer in newServers)
				{
					// check if exists
					bool exists = false;
                    foreach (RdsServer server in servers)
					{
                        if (server.Id == newServer.Id)
						{
							exists = true;
							break;
						}
					}

					if (exists)
						continue;

                    servers.Add(newServer);
				}
			}

            gvServers.DataSource = servers;
            gvServers.DataBind();
		}

        protected List<RdsServer> GetGridViewServers(SelectedState state)
        {
            List<RdsServer> servers = new List<RdsServer>();
            for (int i = 0; i < gvServers.Rows.Count; i++)
            {
                GridViewRow row = gvServers.Rows[i];
                CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                if (chkSelect == null)
                    continue;

                RdsServer server = new RdsServer();
                server.Id = (int)gvServers.DataKeys[i][0];
                server.FqdName = ((Literal)row.FindControl("litFqdName")).Text;

                if (state == SelectedState.All ||
                    (state == SelectedState.Selected && chkSelect.Checked) ||
                    (state == SelectedState.Unselected && !chkSelect.Checked))
                    servers.Add(server);
            }

            return servers;
        }

        protected List<RdsServer> GetPopUpGridViewServers()
        {
            List<RdsServer> servers = new List<RdsServer>();
            for (int i = 0; i < gvPopupServers.Rows.Count; i++)
            {
                GridViewRow row = gvPopupServers.Rows[i];
                CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                if (chkSelect == null)
                    continue;

                if (chkSelect.Checked)
                {
                    servers.Add(new RdsServer
                    {
                        Id = (int)gvPopupServers.DataKeys[i][0],
                        FqdName = ((Literal)row.FindControl("litName")).Text
                    });
                }
            }

            return servers;

        }

		protected void cmdSearch_Click(object sender, ImageClickEventArgs e)
		{
			BindPopupServers();
		}

        protected SortDirection Direction
        {
            get { return ViewState[DirectionString] == null ? SortDirection.Descending : (SortDirection)ViewState[DirectionString]; }
            set { ViewState[DirectionString] = value; }
        }

        protected static int CompareAccount(RdsServer server1, RdsServer server2)
        {
            return string.Compare(server1.FqdName, server2.FqdName);
        }
	}
}