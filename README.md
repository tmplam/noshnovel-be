# NoshNovel API

## Introduction

-   NoshNovel is a project that provides novel related api.
-   The project doesn't have any database, the data it uses is crawled from other pages like *[truyenfull.vn](https://truyenfull.vn/ 'Truyện full')*, *[truyen.tangthuvien.vn](https://truyen.tangthuvien.vn/ 'Tàng thư viện')*, ...
-   This project allows you to add / remove a crawled server in runtime (hot plug) as well as add / remove a novel download format.

## How to add dll plugin file to project?

> 1. Go to `NoshNovel.API/PluginFiles`.
> 2. If you want to add novel crawler server plugin then go to `NovelCrawlers` else go to `NovelDownloadFormats` to add plugin file for novel download extension.
> 3. Add dll file that has a class implemented all method of the provided interface.
> 4. If you want to remove the plugin, just delete the dll file.

**Notes:**

> -   If you want to create a plugin for novel crawler server, you have to create a class that implement the `INovelCrawler` interface in `NosNovel.Plugins` project. You have to add host name for the crawler by using NoverServerAttribute.
> -   If you want to create a plugin for novel download extension, you have to create a class that implement the `INovelDownload` interface in `NosNovel.Plugins` project. You have to add file extension name for the class by using FileExtensionAttribute.
> -   It is recommended that you use the available HtmlAgilityPack Package in `NoshNovel.Plugins` project. If not, you have to move the .dll file of the package that you to the build output directory so that the program can load it.
