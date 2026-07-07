import { UMB_ADVANCED_SETTINGS_MENU_ALIAS as e } from "@umbraco-cms/backoffice/settings";
import { UMB_WORKSPACE_CONDITION_ALIAS as a } from "@umbraco-cms/backoffice/workspace";
const s = [
  {
    type: "entryPoint",
    name: "Robots.txt Entry Point",
    alias: "Casko.RobotsTxtForUmbraco.EntryPoint",
    js: () => import("./entrypoint-Cljvn-9A.js")
  }
], t = "Casko.RobotsTxtForUmbraco.Workspace.RobotsTxt", o = "casko-robots-txt", n = [
  {
    type: "workspace",
    kind: "default",
    name: "Robots.txt Workspace",
    alias: t,
    meta: {
      entityType: o,
      headline: "Robots.txt"
    }
  },
  {
    type: "workspaceView",
    name: "Robots.txt Configuration Workspace View",
    alias: "Casko.RobotsTxtForUmbraco.WorkspaceView.Configuration",
    element: () => import("./configuration-workspace-view.element-D_FEk-kD.js"),
    weight: 500,
    meta: {
      label: "Configuration",
      pathname: "configuration",
      icon: "icon-list"
    },
    conditions: [
      {
        alias: a,
        match: t
      }
    ]
  },
  {
    type: "menuItem",
    name: "Robots.txt Settings Menu Item",
    alias: "Casko.RobotsTxtForUmbraco.MenuItem.RobotsTxt",
    weight: 510,
    meta: {
      label: "Robots.txt",
      icon: "icon-code",
      entityType: o,
      menus: [e]
    }
  }
], m = [
  ...s,
  ...n
];
export {
  m as manifests
};
//# sourceMappingURL=robots-txt-for-umbraco.js.map
