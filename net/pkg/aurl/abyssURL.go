package aurl

import (
	"net/url"
	"strings"
)

type AbyssURL struct {
	Hash string
	Body url.URL

	str string
}

func (a *AbyssURL) String() string {
	if a.str == "" {
		a.str = "abyss:" + a.Hash + ":" + a.Body.String()
	}
	return a.str
}

type URLParseError struct{}

func (u *URLParseError) Error() string {
	return "failed to parse abyssURL"
}

func ParseAbyssURL(raw string) (*AbyssURL, error) {
	split := strings.SplitN(raw, ":", 3)
	if len(split) != 3 {
		return nil, &URLParseError{}
	}

	if split[0] != "abyss" {
		return nil, &URLParseError{}
	}

	body, err := url.Parse(split[2])
	if err != nil {
		return nil, err
	}

	return &AbyssURL{
		Hash: split[1],
		Body: *body,
	}, nil
}
