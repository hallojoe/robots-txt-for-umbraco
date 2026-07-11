import { UMB_ADVANCED_SETTINGS_MENU_ALIAS } from "@umbraco-cms/backoffice/settings";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

const ROBOTS_TXT_WORKSPACE_ALIAS = "Casko.RobotsTxtForUmbraco.Workspace.RobotsTxt";
const ROBOTS_TXT_ENTITY_TYPE = "casko-robots-txt";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "workspace",
    kind: "default",
    name: "Robots.txt Workspace",
    alias: ROBOTS_TXT_WORKSPACE_ALIAS,
    meta: {
      entityType: ROBOTS_TXT_ENTITY_TYPE,
      headline: "Robots.txt",
    },
  },
  {
    type: "workspaceView",
    name: "Robots.txt Configuration Workspace View",
    alias: "Casko.RobotsTxtForUmbraco.WorkspaceView.Configuration",
    element: () => import("./configuration-workspace-view.element.js"),
    weight: 500,
    meta: {
      label: "Configuration",
      pathname: "configuration",
      icon: "icon-list",
    },
    conditions: [
      {
        alias: UMB_WORKSPACE_CONDITION_ALIAS,
        match: ROBOTS_TXT_WORKSPACE_ALIAS,
      },
    ],
  },
  {
    type: "menuItem",
    name: "Robots.txt Settings Menu Item",
    alias: "Casko.RobotsTxtForUmbraco.MenuItem.RobotsTxt",
    weight: 510,
    meta: {
      label: "Robots.txt",
      icon: "icon-code",
      entityType: ROBOTS_TXT_ENTITY_TYPE,
      menus: [UMB_ADVANCED_SETTINGS_MENU_ALIAS],
    },
  },
];
