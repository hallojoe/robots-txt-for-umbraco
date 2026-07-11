# Idea for creating the robots txt solution for Umbraco

Name: `Casko.Casko.RobotsTxtForUmbraco`

We need to create a solution for creating robots.txt file(s) for Umbraco. 

The solution should architectual have the same shape as `../Casko.XmlSitemapsForUmbraco`. It should consist of neccesary models interfaces and implementations. NNo code from XML sitemap solution is used in robotstxt solution. XML sitemaps solution is read only inspiration. Robots txt solution is 100% independant of xml sitemaps solution

Configuration idea:

```
  "RobotsTxt": {
    "Enabled": true,
    "RewritesEnabled": true,
    "IncludedContentTypeAliases": [],
    "ExcludedContentTypeAliases": [],
    "ExcludingUrlPropertyAlias": "umbracoNaviHide",
    "ExcludingUrlPropertyValue": "1",
    "IncludedCultures": ["en", "da", "pl"],
    "ExcludedCultures": [],
    "UseDeliveryApiAccessPolicy": false,
    "Storage": {
      "RefreshStaleAfterSeconds": 3600,
      "BackgroundJob": {
        "Enabled": true,
        "IntervalSeconds": 3600
      },
    },
    "Groups": [
    {
        "UserAgents": [ "*" ],
        "Disallow": [
        "/about-us",
        "/contact-us"
        ],
        "Allow": []
    },
    {
        "UserAgents": [ "anthropic-ai" ],
        "Disallow": [
        "/disallowed-path"
        ]
    }
    ],
    "SitemapsKey": "https://example.com/sitemap.xml"
  }
```

Requirements:

- Must support single multihost
- Culture invariant - does not think about culture when but take all  culture URLSs into account 
- Configurable like XML sitemaps soultion
- Default implementation that will build robots txt from nodes that are exluded  by `ExcludingUrlPropertyValue` and `ExcludingUrlPropertyAlias` 

