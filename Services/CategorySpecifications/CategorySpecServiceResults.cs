namespace e_commerce_web_admin.Services.CategorySpecifications;

public sealed record CategorySpecSaveResult(bool Succeeded, string Message);
public sealed record CategorySpecRemoveResult(bool Found, bool Succeeded, string Message);
