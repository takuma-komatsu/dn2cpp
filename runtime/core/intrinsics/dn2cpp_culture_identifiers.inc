// Frozen identifier data supplements the host-locale numeric table. Language
// aliases use ICU's ISO language codes; neutral destinations follow .NET 10.
// Numeric rows were measured with .NET 10, Apple ICU and upstream ICU 78.
// Apple ICU resolves some unknown language-country combinations by country.

struct Dn2CppCultureNameMapping
{
    const char16_t* name;
    const char16_t* target;
};

// ICU replaces grandfathered prefixes before parsing extension syntax. Some
// replacements introduce private use, which must prevent a later nu override.
// Source: unicode-org/icu, icu4c/source/common/uloc_tag.cpp, LEGACY.
static const Dn2CppCultureNameMapping kCultureNumberingLegacyNames[] = {
    { u"art-lojban", u"jbo" },
    { u"en-gb-oed", u"en-gb-oxendict" },
    { u"i-ami", u"ami" },
    { u"i-bnn", u"bnn" },
    { u"i-hak", u"hak" },
    { u"i-klingon", u"tlh" },
    { u"i-lux", u"lb" },
    { u"i-navajo", u"nv" },
    { u"i-pwn", u"pwn" },
    { u"i-tao", u"tao" },
    { u"i-tay", u"tay" },
    { u"i-tsu", u"tsu" },
    { u"no-bok", u"nb" },
    { u"no-nyn", u"nn" },
    { u"sgn-be-fr", u"sfb" },
    { u"sgn-be-nl", u"vgt" },
    { u"sgn-ch-de", u"sgg" },
    { u"zh-guoyu", u"cmn" },
    { u"zh-hakka", u"hak" },
    { u"zh-min-nan", u"nan" },
    { u"zh-xiang", u"hsn" },
    { u"i-default", u"en-x-i-default" },
    { u"i-enochian", u"und-x-i-enochian" },
    { u"i-mingo", u"see-x-i-mingo" },
    { u"zh-min", u"nan-x-zh-min" },
};

static const Dn2CppCultureNameMapping kCultureLanguageAliases[] = {
    { u"afr", u"af" },
    { u"amh", u"am" },
    { u"ara", u"ar" },
    { u"aze", u"az" },
    { u"bel", u"be" },
    { u"bul", u"bg" },
    { u"ben", u"bn" },
    { u"cat", u"ca" },
    { u"ces", u"cs" },
    { u"dan", u"da" },
    { u"deu", u"de" },
    { u"ell", u"el" },
    { u"eng", u"en" },
    { u"spa", u"es" },
    { u"est", u"et" },
    { u"eus", u"eu" },
    { u"fas", u"fa" },
    { u"fin", u"fi" },
    { u"fra", u"fr" },
    { u"gle", u"ga" },
    { u"glg", u"gl" },
    { u"heb", u"he" },
    { u"hin", u"hi" },
    { u"hrv", u"hr" },
    { u"hun", u"hu" },
    { u"hye", u"hy" },
    { u"ind", u"id" },
    { u"isl", u"is" },
    { u"ita", u"it" },
    { u"jpn", u"ja" },
    { u"kat", u"ka" },
    { u"kaz", u"kk" },
    { u"kor", u"ko" },
    { u"lit", u"lt" },
    { u"lav", u"lv" },
    { u"mon", u"mn" },
    { u"mar", u"mr" },
    { u"msa", u"ms" },
    { u"nob", u"nb" },
    { u"nld", u"nl" },
    { u"nno", u"nn" },
    { u"nor", u"no" },
    { u"pol", u"pl" },
    { u"por", u"pt" },
    { u"ron", u"ro" },
    { u"rus", u"ru" },
    { u"sme", u"se" },
    { u"slk", u"sk" },
    { u"slv", u"sl" },
    { u"srp", u"sr" },
    { u"swe", u"sv" },
    { u"swa", u"sw" },
    { u"tam", u"ta" },
    { u"tel", u"te" },
    { u"tha", u"th" },
    { u"tur", u"tr" },
    { u"ukr", u"uk" },
    { u"urd", u"ur" },
    { u"vie", u"vi" },
    { u"zho", u"zh" },
};

static const Dn2CppCultureNameMapping kCultureSpecificNames[] = {
    { u"af", u"af-ZA" },
    { u"am", u"am-ET" },
    { u"ar", u"ar-SA" },
    { u"az", u"az-Latn-AZ" },
    { u"be", u"be-BY" },
    { u"bg", u"bg-BG" },
    { u"bn", u"bn-BD" },
    { u"ca", u"ca-ES" },
    { u"cs", u"cs-CZ" },
    { u"da", u"da-DK" },
    { u"de", u"de-DE" },
    { u"el", u"el-GR" },
    { u"en", u"en-US" },
    { u"es", u"es-ES" },
    { u"et", u"et-EE" },
    { u"eu", u"eu-ES" },
    { u"fa", u"fa-IR" },
    { u"fi", u"fi-FI" },
    { u"fil", u"fil-PH" },
    { u"fr", u"fr-FR" },
    { u"ga", u"ga-IE" },
    { u"gl", u"gl-ES" },
    { u"he", u"he-IL" },
    { u"hi", u"hi-IN" },
    { u"hr", u"hr-HR" },
    { u"hu", u"hu-HU" },
    { u"hy", u"hy-AM" },
    { u"id", u"id-ID" },
    { u"is", u"is-IS" },
    { u"it", u"it-IT" },
    { u"ja", u"ja-JP" },
    { u"ka", u"ka-GE" },
    { u"kk", u"kk-KZ" },
    { u"ko", u"ko-KR" },
    { u"lt", u"lt-LT" },
    { u"lv", u"lv-LV" },
    { u"mn", u"mn-MN" },
    { u"mr", u"mr-IN" },
    { u"ms", u"ms-MY" },
    { u"nb", u"nb-NO" },
    { u"nl", u"nl-NL" },
    { u"nn", u"nn-NO" },
    { u"no", u"nb-NO" },
    { u"pl", u"pl-PL" },
    { u"pt", u"pt-BR" },
    { u"ro", u"ro-RO" },
    { u"ru", u"ru-RU" },
    { u"se", u"se-NO" },
    { u"sk", u"sk-SK" },
    { u"sl", u"sl-SI" },
    { u"sr", u"sr-Latn-RS" },
    { u"sv", u"sv-SE" },
    { u"sw", u"sw-KE" },
    { u"ta", u"ta-IN" },
    { u"te", u"te-IN" },
    { u"th", u"th-TH" },
    { u"tr", u"tr-TR" },
    { u"uk", u"uk-UA" },
    { u"ur", u"ur-PK" },
    { u"vi", u"vi-VN" },
    { u"zh", u"zh-CN" },
    { u"zh-Hans", u"zh-CN" },
    { u"zh-Hant", u"zh-HK" },
    { u"sr-Latn", u"sr-Latn-RS" },
};

struct Dn2CppCultureSortIdentifier
{
    const char16_t* name;
    int32_t lcid;
};

static const Dn2CppCultureSortIdentifier kCultureSortIdentifiers[] = {
    { u"de-DE_phoneb", 66567 },
    { u"es-ES_tradnl", 1034 },
    { u"zh-CN_stroke", 133124 },
};

static const Dn2CppCultureRow kCultureIdentifierFormats[] = {
#if defined(__APPLE__)
    { u"de-NO", u",", u"\u00a0", u"-", u"NaN", u"-\u221e", u"kr", u"%", 4096, 2, 3, 8, 0, 0, 0, 3, -1 },
#else
    { u"de-NO", u",", u".", u"-", u"NaN", u"-\u221e", u"NOK", u"%", 4096, 2, 3, 8, 0, 0, 0, 3, -1 },
#endif
#if defined(__APPLE__)
    { u"de-FOO", u".", u",", u"-", u"NaN", u"-\u221e", u"\u00a4", u"%", 4096, 2, 3, 8, 0, 0, 0, 3, -1 },
#else
    { u"de-FOO", u",", u".", u"-", u"NaN", u"-\u221e", u"\u00a4", u"%", 4096, 2, 3, 8, 0, 0, 0, 3, -1 },
#endif
    { u"zh-Hans-HK", u".", u",", u"-", u"NaN", u"-\u221e", u"HK$", u"%", 4096, 2, 0, 1, 1, 1, 0, 3, -1 },
#if defined(__APPLE__)
    { u"zh-Hans-TW", u".", u",", u"-", u"NaN", u"-\u221e", u"$", u"%", 4096, 2, 0, 1, 1, 1, 0, 3, -1 },
#else
    { u"zh-Hans-TW", u".", u",", u"-", u"NaN", u"-\u221e", u"NT$", u"%", 4096, 2, 0, 1, 1, 1, 0, 3, -1 },
#endif
    { u"bn-BD", u".", u",", u"-", u"NaN", u"-\u221e", u"\u09f3", u"%", 2117, 2, 1, 5, 1, 1, 0, 3, 2 },
#if defined(__APPLE__)
    { u"sr-Latn-RS", u",", u".", u"-", u"NaN", u"-\u221e", u"RSD", u"%", 9242, 0, 3, 8, 1, 1, 0, 3, -1 },
#else
    { u"sr-Latn-RS", u",", u".", u"-", u"NaN", u"-\u221e", u"RSD", u"%", 9242, 2, 3, 8, 1, 1, 0, 3, -1 },
#endif
    { u"zh-Hans", u".", u",", u"-", u"NaN", u"-\u221e", u"\u00a4", u"%", 4, 2, 0, 1, 1, 1, 1, 3, -1 },
    { u"zh-Hant", u".", u",", u"-", u"\u975e\u6578\u503c", u"-\u221e", u"\u00a4", u"%", 31748, 2, 0, 1, 1, 1, 1, 3, -1 },
    { u"de-Latn", u",", u".", u"-", u"NaN", u"-\u221e", u"\u00a4", u"%", 4096, 2, 3, 8, 0, 0, 1, 3, -1 },
};
