namespace Unfucked.HTTP.Config;

public interface Feature: Registrable {

    ValueTask OnBeforeRequest(IUnfuckedHttpHandler handler);

}