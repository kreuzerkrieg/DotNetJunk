#pragma once
#include "geo_item.h"
class ISO3166_API ip_geo_item
{
public:
	ip_geo_item(
		geo_item &g_item
		);
	virtual ~ip_geo_item(void);
	void set_start_ip(
		const wstring &ip
		);
	void set_end_ip(
		const wstring &ip
		);
	void set_start_ip(
		const string &ip
		);
	void set_end_ip(
		const string &ip
		);
public:
	typedef enum connection_type
	{
		dialup,
		mobile,
		wireless,
		cable,
		dsl,
		xdsl,
		broadband,
		t1,
		t3,
		oc3,
		oc12,
		satellite,
		connection_type_size
	};
private:
	unsigned long ip_to_long(
		const wstring &ip
		) const;
public:
	unsigned long	m_start_ip;
	unsigned long	m_end_ip;
	connection_type	m_connection_type;
	string			m_isp_name;
	const geo_item	&m_geo_item;
};

