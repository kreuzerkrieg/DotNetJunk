#pragma once
#include "iso3166_country.h"
class ISO3166_API geo_item
{
public:
	geo_item(
		unsigned short country_code
		);
	geo_item(
		const wstring &country_name
		);
	virtual ~geo_item(
		void
		);
	unsigned int get_hash(
	) const;
public:
	const iso3166_country	&m_country;
	string					m_region;
	string					m_city;
	unsigned int			m_country_conf;
	unsigned int			m_region_conf;
	unsigned int			m_city_conf;
	unsigned int			m_metro_code;
	double					m_latitude;
	double					m_longitude;
	unsigned int			m_country_code;
	unsigned int			m_region_code;
	unsigned int			m_city_code;
	unsigned int			m_continent_code;
	unsigned int			m_area_code;
	unsigned int			m_zip_code;
	__time64_t				m_gmt_offset;
	bool					m_in_dst;
	string					m_zip_code_text;
};

