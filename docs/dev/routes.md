# Routes

Use `MapAreaControllerRoute` instead of `MapControllerRoute` with manual area defaults — it constrains URL generation to only match controllers that belong to the specified area, preventing tag helpers from generating incorrect area-prefixed URLs.
